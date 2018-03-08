using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using OpenMPTSharp;
using GMESharp;
using HivelySharp;
using VGMPlaySharp;

namespace TrackerBot.Modules
{
    class Music : ModuleBase
    {
		private readonly AudioStreamer m_audioStreamer;
		private readonly Logger m_logger;

		public Music ( AudioStreamer a_audioStreamer, Logger a_logger )
		{
			m_audioStreamer = a_audioStreamer;
			m_logger = a_logger;
		}

		[Command ( "mptver" )]
		public async Task Version ()
		{
			await Context.Channel.SendMessageAsync ( $"{Context.User.Mention} openmpt_get_library_version: {Modplug.GetLibraryVersion ()}" );
		}

		private class StreamException : Exception
		{
			public StreamException ( string a_msg, Exception a_inner = null ) :
				base ( a_msg, a_inner ) {}
		}

		public async Task<Stream> PlayMod ( string a_songName )
		{
			byte[] dat;
			try
			{
				dat = await File.ReadAllBytesAsync ( Path.Combine ( "mod", a_songName ) );
				return new Modplug.ModStream ( dat );
			}
			catch ( IOException ex )
			{
				throw new StreamException ( $"File open failed: {ex.Message}", ex );
			}
			catch ( Modplug.LibException ex )
			{
				throw new StreamException ( $"libopenmpt {ex.Message}", ex );
			}
		}
		
		public async Task<Stream> PlayGme ( string a_songName )
		{
			try
			{
				return new Gme.VgmStream ( Path.Combine ( "gme", a_songName ) );
			}
			catch ( Gme.LibException ex )
			{
				throw new StreamException ( $"{ex.Message}", ex );
			}
		}
		
		public async Task<Stream> PlayHvl ( string a_songName )
		{
			try
			{
				return new Hively.HvlStream ( Path.Combine ( "hvl", a_songName ) );
			}
			catch ( Hively.OpenException ex )
			{
				throw new StreamException ( $"hively didn't like that", ex );
			}
		}

		public async Task<Stream> PlayVgm ( string a_songName )
		{
			try
			{
				return new VgmPlay.VgmStream ( Path.Combine ( "vgm", a_songName ) );
			}
			catch ( Hively.OpenException ex )
			{
				throw new StreamException ( $"{ex.Message}", ex );
			}
		}

		[Command ( "play" ), Summary ( "Play a song I guess." )]
		private async Task Play ( string a_songName )
		{
			var guild = Context.Guild;
			IGuildUser user = await Context.Guild.GetUserAsync ( Context.User.Id );
			var channel = user.VoiceChannel;
			if ( channel == null )
			{
				await Context.Channel.SendMessageAsync ( $"{user.Mention} You must be in a voice channel to use the audio commands." );
				return;
			}

			int dot = a_songName.LastIndexOf ( '.' );
			if ( dot <= 0 || dot >= a_songName.Length - 1 )
			{
				await Context.Channel.SendMessageAsync ( $"{user.Mention} Yeah nah." );
				return;
			}

			Func<string, Task<Stream>> getStream;

			switch ( a_songName.Substring ( dot + 1, a_songName.Length - ( dot + 1 ) ) )
			{
			case ( "mod" ):
			case ( "xm" ):
			case ( "s3m" ):
			case ( "it" ):
			case ( "mptm" ):
				getStream = PlayMod;
				break;

			case ( "spc" ):
			case ( "nsf" ):
				getStream = PlayGme;
				break;

			case ( "hvl" ):
			case ( "ahx" ):
				getStream = PlayHvl;
				break;

			case ( "vgm" ):
			case ( "vgz" ):
				getStream = PlayVgm;
				break;

			default:
				await Context.Channel.SendMessageAsync ( $"{user.Mention} Sorry, that format isn't supported." );
				return;
			}

			bool playing = false;
			try
			{
				try
				{
					await m_audioStreamer.Join ( Context.Guild, channel );
				}
				catch ( TrackerBot.AudioStreamer.AlreadyConnectedException )
				{
					await Context.Channel.SendMessageAsync ( $"{user.Mention} I'm already playing in this server silly." );
					return;
				}

				playing = true;

				try
				{
					using ( Stream stream = await getStream ( a_songName ) )
					{
						if ( stream == null )
						{
							throw new StreamException ( "Generic stream error" );
						}

						await m_audioStreamer.SendAudioAsync ( Context.Guild, stream );
					}
				}
				catch ( StreamException ex )
				{
					m_logger.Log ( LogSeverity.Error, $"StreamException: {ex.Message}\nInner: {ex.InnerException}", "Music" );
					await Context.Channel.SendMessageAsync ( $"{user.Mention} {ex.Message} (check console for details)" );
				}
			}
			finally
			{
				if ( playing )
				{
					await m_audioStreamer.Leave ( Context.Guild );
				}
			}
		}

		[Command ( "stop" )]
		public async Task Leave ()
		{
			var guild = Context.Guild;
			IGuildUser user = await Context.Guild.GetUserAsync ( Context.User.Id );
			var channel = user.VoiceChannel;
			if ( channel == null )
			{
				await Context.Channel.SendMessageAsync ( $"{user.Mention} You must be in a voice channel to use the audio commands." );
				return;
			}

			await m_audioStreamer.StopStream ( guild.Id );
		}

		/*
		[Command ( "leave" )]
		public async Task Leave ()
		{
			var channel = (Context.User as IGuildUser ).VoiceChannel;
			await m_audioStreamer.Join ( Context.Guild, channel );
			await m_audioStreamer.Leave ( Context.Guild );
		}
		*/
    }
}
