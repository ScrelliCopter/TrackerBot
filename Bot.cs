using System;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Discord;
using Discord.WebSocket;
using Discord.Commands;

namespace TrackerBot
{
	internal class Bot
	{
		private DiscordSocketClient m_client;

		public async Task Start ()
		{
			if ( m_client != null )
			{
				throw new Exception ( "Bot is already started" );
			}

			// Load config file.
			try
			{
				Global.LoadConfig ();
			}
			catch ( Exception )
			{
				try
				{
					Global.SaveConfig ();
				}
				catch ( Exception )
				{
					throw new Exception ( "Error trying to write config file, do you have permission?" );
				}

				throw new Exception ( "Need a config please, there I made one for you now go edit it." );
			}

			if ( String.IsNullOrWhiteSpace ( Global.Config.Token ) )
			{
				throw new Exception ( "Listen lad I need you to actually give me a tokan or I can't connect to discord, ever think of that huh?" );
			}

			// Create client.
			m_client = new DiscordSocketClient ( new DiscordSocketConfig
				{
					LogLevel = LogSeverity.Verbose,
					MessageCacheSize = 1000
				} );

			// Install services.
			IServiceProvider services = new ServiceCollection ()
				.AddSingleton ( m_client )
				.AddSingleton ( new CommandService ( new CommandServiceConfig 
				{
					LogLevel = LogSeverity.Verbose,
					DefaultRunMode = RunMode.Async,
					CaseSensitiveCommands = false
				} ) )
				.AddSingleton<Logger> ()
				.AddSingleton<CommandHandler> ()
				.AddSingleton<AudioStreamer> ()
				.BuildServiceProvider ();

			// Init services that need it.
			services.GetRequiredService<Logger> ();
			services.GetRequiredService<AudioStreamer> ();
			services.GetRequiredService<CommandHandler> ();

			// Add modules.
			var cmd = services.GetRequiredService<CommandService> ();
			await cmd.AddModuleAsync<Modules.Test> ();
			await cmd.AddModuleAsync<Modules.Help> ();
			await cmd.AddModuleAsync<Modules.Admin> ();
			await cmd.AddModuleAsync<Modules.Music> ();

			// Connect to discord.
			await m_client.LoginAsync ( TokenType.Bot, Global.Config.Token );
			await m_client.StartAsync ();

			// Set game.
			m_client.Connected += async () =>
			{
				await m_client.SetGameAsync ( Global.Config.Game );
			};

			// Wait until control-c.
			CancellationTokenSource romanCancel = new CancellationTokenSource ();
			Console.CancelKeyPress += ( s, e ) =>
			{
				e.Cancel = true;
				Console.WriteLine ( "Ctrl-C pressed... Goodbye~" );
				romanCancel.Cancel ();
			};
			try
			{
				await Task.Delay ( -1, romanCancel.Token );
			}
			catch ( TaskCanceledException ) {} // Nothing to see here...
			finally
			{

			}
		}
	}
}
