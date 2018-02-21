using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using OpenMPTSharp;
using GMESharp;

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

		[Command ( "playmod" ), Summary ( "try playing a song with libopenmpt." )]
		public async Task PlayMod ()
		{
			var guild = Context.Guild;
			IGuildUser user = await Context.Guild.GetUserAsync ( Context.User.Id );
			var channel = user.VoiceChannel;
			if ( channel == null )
			{
				await Context.Channel.SendMessageAsync ( $"{user.Mention} You must be in a voice channel to use the audio commands." );
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

				byte[] dat;
				try
				{
					dat = await File.ReadAllBytesAsync ( "mod/when.s3m" );
				}
				catch ( IOException ex )
				{
					m_logger.Log ( LogSeverity.Error, $"File open failed: {ex.Message}", "Music" );
					await Context.Channel.SendMessageAsync ( $"{user.Mention} Failed to open file, (details in console)" );
					return;
				}

				using ( var modStream = new Modplug.ModStream ( dat ) )
				{
					await m_audioStreamer.SendAudioAsync ( Context.Guild, modStream );
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

		[Command ( "playvgm" ), Summary ( "try playing a song with libgme." )]
		public async Task PlayVgm ()
		{
			var guild = Context.Guild;
			IGuildUser user = await Context.Guild.GetUserAsync ( Context.User.Id );
			var channel = user.VoiceChannel;
			if ( channel == null )
			{
				await Context.Channel.SendMessageAsync ( $"{user.Mention} You must be in a voice channel to use the audio commands." );
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

				using ( var vgmStream = new Gme.VgmStream ( "vgm/extremeliftoff.vgm" ) )
				{
					await m_audioStreamer.SendAudioAsync ( Context.Guild, vgmStream );
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
    }
}
