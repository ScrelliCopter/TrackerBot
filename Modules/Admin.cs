using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Discord.WebSocket;
using Discord.Commands;

namespace TrackerBot.Modules
{
    class Admin : ModuleBase
    {
		private readonly DiscordSocketClient m_client;

		public Admin ( DiscordSocketClient a_client )
		{
			m_client = a_client;
		}

		#region Config

		[Command ( "cfg-ls" )]
		public async Task CfgLs ()
		{
			if ( Context.User.Id != Global.Config.Admin )
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
			if ( Context.User.Id != Global.Config.Admin )
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

			var value = p.GetMethod.Invoke ( Global.Config, null );
			await channel.SendMessageAsync ( $"Value of `{a_token}` is `{value.ToString ()}` of type `{p.PropertyType}`." );
		}

		[Command ( "cfg-set" )]
		public async Task CfgSet ( string a_token, [Remainder]string a_value )
		{
			if ( Context.User.Id != Global.Config.Admin )
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
				p.SetMethod.Invoke ( Global.Config, new object[] { value } );
				Global.SaveConfig ();
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
			if ( Context.User.Id != Global.Config.Admin )
			{
				return;
			}

			await m_client.CurrentUser.ModifyAsync ( p => p.Username = a_name );
			await Context.Channel.SendMessageAsync ( $"{Context.User.Mention} Username updated. :ok_hand:" );
		}

		[Command ( "user-avatar" )]
		public async Task UserAvatar ( [Remainder]string a_name )
		{
			if ( Context.User.Id != Global.Config.Admin )
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

		[Command ( "user-game" )]
		public async Task UserGame ( [Remainder]string a_game )
		{
			if ( Context.User.Id != Global.Config.Admin )
			{
				return;
			}

			await m_client.SetGameAsync ( a_game );
			Global.Config.Game = a_game;
			Global.SaveConfig ();
			await Context.Channel.SendMessageAsync ( $"{Context.User.Mention} Game updated. :ok_hand:" );
		}

		[Command ( "guild-list" )]
		public async Task GuildList ()
		{
			if ( Context.User.Id != Global.Config.Admin )
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
			if ( Context.User.Id != Global.Config.Admin )
			{
				return;
			}

			var g = m_client.GetGuild ( a_id );
			if ( g == null )
			{
				await Context.Channel.SendMessageAsync ( $"{Context.User.Mention} No joined Guild matching yonder ID." );
			}

			string name = g.Name;
			await g.LeaveAsync ();
			await Context.Channel.SendMessageAsync ( $"{Context.User.Mention} Left {name} :wave:" );
		}
	}
}
