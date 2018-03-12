using System.Text;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;

namespace TrackerBot.Modules
{
    class Help : ModuleBase
    {
		private readonly CommandService m_cmdSrv;
		private readonly ConfigService m_cfgSrv;

		public Help ( CommandService a_cmdSrv, ConfigService a_cfgSrv )
		{
			m_cmdSrv = a_cmdSrv;
			m_cfgSrv = a_cfgSrv;
		}

		[Command ( "help" ), Summary ( "Displays this text." )]
		public async Task HelpCmd ( [Summary ( "command to display help for, or blank for list" )]string a_name = null )
		{
			StringBuilder sb = new StringBuilder ();

			if ( string.IsNullOrWhiteSpace ( a_name ) )
			{
				sb.Append ( Context.User.Mention );
				sb.AppendLine ( " here's a list of commands:" );
				sb.AppendLine ( "```" );

				
				foreach ( var cmd in m_cmdSrv.Commands )
				{
					if ( cmd.Summary == null )
					{
						continue;
					}

					sb.Append ( cmd.Name );
					if ( !string.IsNullOrEmpty ( cmd.Summary ) )
					{
						sb.Append ( ' ' );
						for ( int i = cmd.Name.Length; i < 10; ++i )
						{
							sb.Append ( '-' );
						}
						sb.Append ( ' ' );
						sb.Append ( cmd.Summary );
					}
					sb.AppendLine ();
				}

				sb.AppendLine ( "```" );
				sb.Append ( $"Type `{m_cfgSrv.Config.Prefix}help <command name>` to see detailed information on a command." );

				await Context.Channel.SendMessageAsync ( sb.ToString () );
			}
			else
			{
				var cmd = m_cmdSrv.Commands.FirstOrDefault ( ( e ) => e.Name == a_name );
				if ( cmd == null )
				{
					await Context.Channel.SendMessageAsync ( $"{Context.User.Mention} unable to locate suitable command." );
					return;
				}

				sb.Append ( Context.User.Mention );
				sb.Append ( $" found help text for command `{m_cfgSrv.Config.Prefix}" );
				sb.Append ( cmd.Name );
				sb.AppendLine ( "`:" );

				if ( cmd.Summary != null )
				{
					sb.Append ( "Summary: " );
					sb.AppendLine ( cmd.Summary );
				}

				sb.Append ( $"Usage: `{m_cfgSrv.Config.Prefix}" );
				sb.Append ( cmd.Name );
				foreach ( var arg in cmd.Parameters )
				{
					sb.Append ( ' ' );
					sb.Append ( arg.IsOptional ? '[' : '<' );
					sb.Append ( arg.Summary == null ? arg.Name : arg.Summary );
					sb.Append ( arg.IsOptional ? ']' : '>' );
				}
				sb.Append ( "`" );

				await Context.Channel.SendMessageAsync ( sb.ToString () );
			}
		}
    }
}
