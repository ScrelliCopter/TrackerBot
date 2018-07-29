using System;
using System.IO;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Audio;

namespace TrackerBot
{
    public class AudioStreamer
    {
		public class AlreadyConnectedException : Exception { }
		public class NotConnectedException : Exception { }

		private readonly Logger m_logger;

		public AudioStreamer ( Logger a_logger )
		{
			m_logger = a_logger;
		}

		private readonly ConcurrentDictionary<ulong, IAudioClient> m_connectedChannels = new ConcurrentDictionary<ulong, IAudioClient> ();
		private readonly ConcurrentDictionary<ulong, CancellationTokenSource> m_cancellationTokens = new ConcurrentDictionary<ulong, CancellationTokenSource> ();

		public async Task Join ( IGuild a_guild, IVoiceChannel a_chan )
		{
			if ( a_guild.Id != a_chan.GuildId )
			{
				throw new Exception ( "Fuck you" );
			}

			if ( m_connectedChannels.ContainsKey ( a_guild.Id ) )
			{
				throw new AlreadyConnectedException ();
			}
			
			if ( !m_connectedChannels.TryAdd ( a_guild.Id, null ) )
			{
				// ???
				return;
			}

			try
			{
				var audioClient = await a_chan.ConnectAsync ();
				m_connectedChannels.TryUpdate ( a_guild.Id, audioClient, null );
				m_logger.Log ( LogSeverity.Info, $"Connected to voice channel {a_chan.GuildId}", "AudioStreamer" );
			}
			finally
			{
				IAudioClient audioClient;
				if ( m_connectedChannels.TryGetValue ( a_guild.Id, out audioClient ) )
				{
					if ( audioClient == null )
					{
						m_connectedChannels.TryRemove ( a_guild.Id, out audioClient );
					}
				}
			}
		}

		public async Task Leave ( IGuild a_guild )
		{
			IAudioClient audioClient;
			if ( m_connectedChannels.TryRemove ( a_guild.Id, out audioClient ) )
			{
				if ( audioClient != null )
				{
					await audioClient.StopAsync ();
				}
			}
			else
			{
				throw new NotConnectedException ();
			}
		}

		public async Task SendAudioAsync ( IGuild a_guild, Stream a_stream )
		{
			IAudioClient audioClient;
			if ( !m_connectedChannels.TryGetValue ( a_guild.Id, out audioClient ) )
			{
				throw new NotConnectedException ();
			}

			CancellationTokenSource tokenSource = new CancellationTokenSource ();
			if ( !m_cancellationTokens.TryAdd ( a_guild.Id, tokenSource ) )
			{
				if ( !m_cancellationTokens.TryGetValue ( a_guild.Id, out tokenSource ) )
				{
					throw new Exception ( "Cancellation token cannot be null!" );
				}
			}

			using ( var voiceStream = audioClient.CreatePCMStream ( AudioApplication.Music, 128 * 1024 ) )
			{
				try
				{
					//await a_stream.CopyToAsync ( voiceStream, 192000 * 1, a_cancel );
					await a_stream.CopyToAsync ( voiceStream, ( 48000 * 4 ) / 4, tokenSource.Token );
				}
				catch ( TaskCanceledException ) { }
				catch ( OperationCanceledException ) { }
				finally
				{
					m_cancellationTokens.TryRemove ( a_guild.Id, out tokenSource );
					await voiceStream.FlushAsync ();
				}
			}
		}

		public async Task<bool> StopStream ( ulong a_guild ) => await Task.Run ( () =>
		{
			if ( m_cancellationTokens.TryRemove ( a_guild, out CancellationTokenSource ct ) )
			{
				ct.Cancel ();
				return true;
			}

			return false;
		} );
    }
}
