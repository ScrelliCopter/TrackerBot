using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discord.Commands;

namespace TrackerBot.Modules
{
    class Admin : ModuleBase
    {
		private readonly DiscordSocketClient m_client;
		private readonly ConfigService m_cfgSrv;
		private readonly Logger m_logger;

		public Admin ( DiscordSocketClient a_client, ConfigService a_cfgSrv, Logger a_logger )
		{
			m_client = a_client;
			m_cfgSrv = a_cfgSrv;
			m_logger = a_logger;
		}

		#region Config

		[Command ( "cfg-ls" )]
		public async Task CfgLs ()
		{
			if ( Context.User.Id != m_cfgSrv.Config.Admin )
			{
				return;
			}

			var channel = await Context.User.GetOrCreateDMChannelAsync ();
			
			var sb = new StringBuilder ();
			sb.AppendLine ( "```" );
			foreach ( var p in typeof(Config).GetProperties () )
			{
				sb.AppendLine ( $"{p.Name} : {p.PropertyType.ToString ()}" );
			}
			sb.Append ( "```" );
			
			await channel.SendMessageAsync ( sb.ToString () );
		}

		[Command ( "cfg-get" )]
		public async Task CfgGet ( string a_token )
		{
			if ( Context.User.Id != m_cfgSrv.Config.Admin )
			{
				return;
			}

			var channel = await Context.User.GetOrCreateDMChannelAsync ();

			var p = typeof(Config).GetProperty ( a_token );
			if ( p == null )
			{
				await channel.SendMessageAsync ( $"Property `{a_token}` not found." );
				return;
			}

			var value = p.GetMethod.Invoke ( m_cfgSrv.Config, null );
			await channel.SendMessageAsync ( $"Value of `{a_token}` is `{value.ToString ()}` of type `{p.PropertyType}`." );
		}

		[Command ( "cfg-set" )]
		public async Task CfgSet ( string a_token, [Remainder]string a_value )
		{
			if ( Context.User.Id != m_cfgSrv.Config.Admin )
			{
				return;
			}

			var p = typeof(Config).GetProperty ( a_token );
			if ( p == null )
			{
				await Context.Channel.SendMessageAsync ( $"{Context.User.Mention} Property `{a_token}` not found." );
				return;
			}

			try
			{
				var value = Convert.ChangeType ( a_value, p.PropertyType );
				p.SetMethod.Invoke ( m_cfgSrv.Config, new object[] { value } );
				m_cfgSrv.SaveConfig ();
				await Context.Channel.SendMessageAsync ( $"{Context.User.Mention} Property updated. :ok_hand:" );
			}
			catch ( FormatException )
			{
				await Context.Channel.SendMessageAsync ( $"{Context.User.Mention} Value cannot be converted to `{p.PropertyType}`: Invalid format." );
			}
			catch ( InvalidCastException )
			{
				await Context.Channel.SendMessageAsync ( $"{Context.User.Mention} Value cannot be converted to `{p.PropertyType}`: Bad cast." );
			}
			catch ( OverflowException )
			{
				await Context.Channel.SendMessageAsync ( $"{Context.User.Mention} Value out of range silly." );
			}
		}

		#endregion config

		[Command ( "user-name" )]
		public async Task UserName ( [Remainder]string a_name )
		{
			if ( Context.User.Id != m_cfgSrv.Config.Admin )
			{
				return;
			}

			await m_client.CurrentUser.ModifyAsync ( p => p.Username = a_name );
			await Context.Channel.SendMessageAsync ( $"{Context.User.Mention} Username updated. :ok_hand:" );
		}

		/*
		[Command ( "user-avatar" )]
		public async Task UserAvatar ( [Remainder]string a_name )
		{
			if ( Context.User.Id != m_cfgSrv.Config.Admin )
			{
				return;
			}
			
			string basePath = Path.GetFullPath ( "avatar/" );
			string uri = Path.GetFullPath ( Path.Combine ( basePath, a_name ) );
			if ( !uri.StartsWith ( basePath ) )
			{
				await Context.Channel.SendMessageAsync ( $"{Context.User.Mention} Yeah nah." );
				return;
			}
			
			try
			{
				using ( var fileStream = File.OpenRead ( uri ) )
				{
					var img = new Discord.Image ( fileStream );
					await Context.Client.CurrentUser.ModifyAsync ( p => p.Avatar = img );
				}
			}
			catch ( IOException ex )
			{
				await Context.Channel.SendMessageAsync ( $"{Context.User.Mention} Couldn't read {a_name}: {ex.Message}" );
				return;
			}

			await Context.Channel.SendMessageAsync ( $"{Context.User.Mention} Avatar updated. :tada:" );
		}
		*/

		[Command ( "user-game" )]
		public async Task UserGame ( [Remainder]string a_game )
		{
			if ( Context.User.Id != m_cfgSrv.Config.Admin )
			{
				return;
			}

			await m_client.SetGameAsync ( a_game );
			m_cfgSrv.Config.Game = a_game;
			m_cfgSrv.SaveConfig ();
			await Context.Channel.SendMessageAsync ( $"{Context.User.Mention} Game updated. :ok_hand:" );
		}

		[Command ( "user-dm" )]
		public async Task UserDm ( ulong a_userId )
		{
			if ( Context.User.Id != m_cfgSrv.Config.Admin )
			{
				return;
			}

			var u = m_client.GetUser ( a_userId );
			if ( u == null )
			{
				await Context.Channel.SendMessageAsync ( $"{Context.User.Mention} Couldn't find user." );
				return;
			}


			Discord.IDMChannel c = null;
			try
			{
				c = await u.GetOrCreateDMChannelAsync ();
			}
			catch ( Discord.Net.HttpException ex )
			{
				c = null;
				m_logger.Log ( Discord.LogSeverity.Error, $"Discord.Net.HttpException: {ex.Message}", "Admin" );
				//m_logger.Log ( Discord.LogSeverity.Error, $"{ex.ToString ()}", "Admin" );
			}

			if ( c == null )
			{
				await Context.Channel.SendMessageAsync ( $"{Context.User.Mention} Couldn't create a DM channel for that user." );
				return;
			}

			await Context.Channel.SendMessageAsync ( $"{Context.User.Mention} DM channel for `{u.Username}#{u.Discriminator}` is `{c.Id}`." );
		}

		[Command ( "chan-ls" )]
		public async Task ChannelList ( ulong a_id = 0 )
		{
			IGuild g;
			if ( a_id != 0 )
			{
				g = m_client.GetGuild ( a_id );
			}
			else
			{
				g = Context.Guild;
			}

			if ( g == null )
			{
				await Context.Channel.SendMessageAsync ( $"{Context.User.Mention} Can't find that guild." );
				return;
			}

			StringBuilder sb = new StringBuilder ();
			sb.AppendLine ( $"{Context.User.Mention} List of channels in {g.Name}:```" );
			foreach ( var c in await g.GetTextChannelsAsync () )
			{
				sb.AppendLine ( $"{c.Id} - {c.Name}" );
				if ( c.Id == g.DefaultChannelId )
				{
					sb.AppendLine ( " ^ DEFAULT" );
				}
			}
			sb.AppendLine ();
			foreach ( var c in await g.GetVoiceChannelsAsync () )
			{
				sb.AppendLine ( $"{c.Id} - {c.Name}" );
			}
			sb.Append ( "```" );

			await Context.Channel.SendMessageAsync ( sb.ToString () );
		}

		[Command ( "chan-get" )]
		public async Task ChannelGet ( ulong a_chanId = 0 )
		{
			var sc = m_client.GetChannel ( a_chanId );
			var c = sc as IChannel;
			if ( c == null )
			{
				await Context.Channel.SendMessageAsync ( $"{Context.User.Mention} Can't find that channel." );
				return;
			}

			var msgChan = c as IMessageChannel;
			var voipChan = c as IVoiceChannel;

			StringBuilder sb = new StringBuilder ();
			sb.AppendLine ( $"{Context.User.Mention}```" );

			sb.AppendLine (		$"Name:   {c.Name}" );
			sb.AppendLine (		$"ID:     {c.Id}" );
			//sb.AppendLine (		$"Perms:  {}" );

			if ( msgChan != null )
			{
				var pinned = await msgChan.GetPinnedMessagesAsync ();

				sb.AppendLine (	 "Type:   Text" );
				sb.AppendLine (	$"Pinned: {pinned.Count}" );
			}
			else
			if ( voipChan != null )
			{
				int users	= voipChan.GetUsersAsync ().Flatten ().Result.Count ();
				int max		= voipChan.UserLimit ?? 0;
				float rate	= (float)voipChan.Bitrate / 1000f;

				sb.AppendLine (	 "Type:   Audio" );
				sb.AppendLine (	$"Users:  {users}/{max}" );
				sb.AppendLine (	$"Rate:   {rate}" );
			}
			
			sb.Append ( "```" );

			await Context.Channel.SendMessageAsync ( sb.ToString () );
		}

		[Command ( "guild-ls" )]
		public async Task GuildList ()
		{
			if ( Context.User.Id != m_cfgSrv.Config.Admin )
			{
				return;
			}

			var sb = new StringBuilder ();
			sb.AppendLine ( $"{Context.User.Mention}```" );
			foreach ( var g in m_client.Guilds )
			{
				sb.AppendLine ( $"{g.Id} - {g.Name}" );
			}
			sb.Append ( "```" );

			await Context.Channel.SendMessageAsync ( sb.ToString () );
		}

		[Command ( "guild-leave" )]
		public async Task GuildLeave ( ulong a_id )
		{
			if ( Context.User.Id != m_cfgSrv.Config.Admin )
			{
				return;
			}

			var g = m_client.GetGuild ( a_id );
			if ( g == null )
			{
				await Context.Channel.SendMessageAsync ( $"{Context.User.Mention} No joined Guild matching yonder ID." );
				return;
			}

			string name = g.Name;
			await g.LeaveAsync ();
			await Context.Channel.SendMessageAsync ( $"{Context.User.Mention} Left {name} :wave:" );
		}

		[Command ( "say" )]
		public async Task Say ( ulong a_id, [Remainder]string a_msg )
		{
			if ( Context.User.Id != m_cfgSrv.Config.Admin )
			{
				return;
			}

			var c = m_client.GetChannel ( a_id ) as ISocketMessageChannel;
			if ( c == null )
			{
				await Context.Channel.SendMessageAsync ( $"{Context.User.Mention} No channel matching yonder ID." );
				return;
			}
			
			await c.SendMessageAsync ( a_msg );
		}

		[Command ( "announce" )]
		public async Task Announce ( [Remainder]string a_msg )
		{
			if ( Context.User.Id != m_cfgSrv.Config.Admin )
			{
				return;
			}

			ulong uid = m_client.CurrentUser.Id;
			foreach ( var g in m_client.Guilds )
			{
				var c = g.DefaultChannel;
				if ( c != null )
				{
					var u = g.GetUser ( uid );
					if ( u.GetPermissions ( c ).SendMessages )
					{
						m_logger.Log ( Discord.LogSeverity.Info, $"Sending to: `{g.Id}` {g.Name}", "Admin" );
						await c.SendMessageAsync ( a_msg );
					}
				}
			}
		}
	}
}
