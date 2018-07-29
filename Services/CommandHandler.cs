using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discord.Commands;

namespace TrackerBot
{
	public class CommandHandler
	{
		private readonly DiscordSocketClient m_client;
		private readonly CommandService m_cmdSrv;
		private readonly ConfigService m_cfgSrv;
		private readonly Logger	m_logger;
		private IServiceProvider m_provider;

		public CommandHandler ( IServiceProvider a_provider, DiscordSocketClient a_client, CommandService a_cmdSrv, ConfigService a_cfgSrv, Logger a_logger )
		{
			m_client = a_client;
			m_cmdSrv = a_cmdSrv;
			m_cfgSrv = a_cfgSrv;
			m_logger = a_logger;
			m_provider = a_provider;

			m_client.MessageReceived += MessageReceived;
		}

		private async Task MessageReceived ( SocketMessage e )
		{
			var msg = e as SocketUserMessage;
			if ( msg == null )
			{
				return;
			}

			int argIdx = 0;
			if ( msg.HasCharPrefix ( m_cfgSrv.Config.Prefix, ref argIdx ) ||
				msg.HasMentionPrefix ( m_client.CurrentUser, ref argIdx ) )
			{
				IUser usr = msg.Author;
				m_logger.Log ( LogSeverity.Info, $"{msg.Content} called by {usr.Username}#{usr.Discriminator}" );

				var ctx = new CommandContext ( m_client, msg );
				var res = await m_cmdSrv.ExecuteAsync ( ctx, argIdx, m_provider );
				if ( !res.IsSuccess )
				{
					await ErrorHandler ( ctx, res );
				}
			}
		}

		private async Task ErrorHandler ( CommandContext a_ctx, IResult a_res )
		{
			string msg = null;
			switch ( a_res.Error )
			{
				case ( CommandError.Exception ):
					//msg = "Idk what the fuck happened, but this message should not be shown unless something really went shit side up.";
					msg = $"{a_ctx.User.Mention} Something goofed behind the scenes, blame dino's shit code.";
					m_logger.Log ( LogSeverity.Error, $"Exception thrown: {a_res.ErrorReason}" );
					break;
				case ( CommandError.BadArgCount ):
					msg = $"{a_ctx.User.Mention} Wrong number of arguments.";
					break;

				case ( CommandError.ObjectNotFound ):
				case ( CommandError.MultipleMatches ):
				case ( CommandError.UnmetPrecondition ):
				case ( CommandError.ParseFailed ):
					msg = $"{a_ctx.User.Mention} Parse error: " + a_res.ErrorReason;
					break;

				default:
					break;
			}

			if ( msg != null )
			{
				await a_ctx.Channel.SendMessageAsync ( msg );
			}
		}
	}
}
