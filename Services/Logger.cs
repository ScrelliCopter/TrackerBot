using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discord.Commands;

namespace TrackerBot
{
    public class Logger
    {
		private readonly DiscordSocketClient m_client;
		private readonly CommandService m_cmdSrv;

		public Logger ( DiscordSocketClient a_client, CommandService a_cmdSrv )
		{
			m_client = a_client;
			m_cmdSrv = a_cmdSrv;

			m_client.Log += LogCallback;
			m_cmdSrv.Log += LogCallback;
		}

		public void Log ( LogSeverity a_severity, string a_msg, string a_src = null )
		{
			switch ( a_severity )
			{
			case ( LogSeverity.Critical ):
				Console.ForegroundColor = ConsoleColor.Black;
				Console.BackgroundColor = ConsoleColor.Red;
				break;
			case ( LogSeverity.Error ):
				Console.ForegroundColor = ConsoleColor.Red;
				break;
			case ( LogSeverity.Warning ):
				Console.ForegroundColor = ConsoleColor.Yellow;
				break;
			case ( LogSeverity.Info ):
				Console.ForegroundColor = ConsoleColor.White;
				break;
			case ( LogSeverity.Verbose ):
				Console.ForegroundColor = ConsoleColor.Gray;
				break;
			case ( LogSeverity.Debug ):
				Console.ForegroundColor = ConsoleColor.DarkGray;
				break;

			default:
				break;
			}

			if ( a_src != null )
			{
				Console.WriteLine ( $"[{a_severity}] {a_src}: {a_msg}" );
			}
			else
			{
				Console.WriteLine ( $"[{a_severity}] {a_msg}" );
			}
			
			Console.ResetColor ();
		}

		private Task LogCallback ( LogMessage a_msg )
		{
			string msg;
			if ( a_msg.Exception != null )
			{
				msg = $"{a_msg.Message}\nException:\n{a_msg.Exception}";
			}
			else
			{
				msg = a_msg.Message;
			}

			Log ( a_msg.Severity, msg, a_msg.Source );
			return Task.CompletedTask;
		}
    }
}
