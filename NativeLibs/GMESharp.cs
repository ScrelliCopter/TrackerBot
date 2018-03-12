using System;
using System.IO;
using System.Runtime.InteropServices;

namespace GMESharp
{
	public static class Gme
    {
		#region DllImports

		[DllImport ( "libgme", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl )]
		/*gme_err_t*/		private static extern IntPtr gme_open_data (
			/*void const**/		[MarshalAs ( UnmanagedType.LPArray )]byte[] data,
			/*long*/			long size,
			/*Music_Emu***/		[Out]out IntPtr emu_out,
			/*int*/				int sample_rate );

		[DllImport ( "libgme", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl )]
		/*gme_err_t*/		private static extern IntPtr gme_open_file (
			/*const char**/		[MarshalAs ( UnmanagedType.LPStr )]string path,
			/*Music_Emu***/		[Out]out IntPtr emu_out,
			/*int*/				int sample_rate );

		[DllImport ( "libgme", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl )]
		/*void*/			private static extern void gme_delete (
			/*Music_Emu***/		IntPtr emu );

		[DllImport ( "libgme", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl )]
		/*int*/				private static extern int gme_track_count (
			/*Music_Emu**/		IntPtr emu );

		[DllImport ( "libgme", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl )]
		/*gme_err_t*/		private static extern IntPtr gme_start_track (
			/*Music_Emu**/		IntPtr emu,
			/*int*/				int index );

		[DllImport ( "libgme", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl )]
		/*gme_err_t*/		private static extern IntPtr gme_play (
			/*Music_Emu**/		IntPtr emu,
			/*int*/				int count,
			/*short[]*/			[Out]byte[] samp_out );

		[DllImport ( "libgme", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl )]
		/*int*/				private static extern int gme_track_ended (
			/*Music_Emu**/		IntPtr emu );

		[DllImport ( "libgme", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl )]
		/*int*/				private static extern int gme_tell (
			/*Music_Emu**/		IntPtr emu );

		[DllImport ( "libgme", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl )]
		/*const char**/		private static extern IntPtr gme_identify_header (
			/*void const**/		[MarshalAs ( UnmanagedType.LPArray )]byte[] header );

		struct gme_info_t
		{
			// Times in milliseconds; -1 if unknown.
			int length;			// Total length, if file specifies it.
			int intro_length;	// Length of song up to looping section.
			int loop_length;	// Length of looping section.

			// Length if available, otherwise intro_length+loop_length*2 if available,
			// otherwise a default of 150000 (2.5 minutes).
			int play_length;

			// Reserved.
			int i4, i5, i6, i7, i8, i9, i10, i11, i12, i13, i14, i15;

			// Empty string ("") if not available.
			[MarshalAs ( UnmanagedType.LPStr )]string system;
			[MarshalAs ( UnmanagedType.LPStr )]string game;
			[MarshalAs ( UnmanagedType.LPStr )]string song;
			[MarshalAs ( UnmanagedType.LPStr )]string author;
			[MarshalAs ( UnmanagedType.LPStr )]string copyright;
			[MarshalAs ( UnmanagedType.LPStr )]string comment;
			[MarshalAs ( UnmanagedType.LPStr )]string dumper;

			// Reserved.
			[MarshalAs ( UnmanagedType.LPStr )]string s7, s8, s9, s10, s11, s12, s13, s14, s15;
		}

		[DllImport ( "libgme", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl )]
		/*gme_err_t*/		private static extern IntPtr gme_track_info (
			/*Music_Emu**/		IntPtr emu,
			/*gme_info_t***/	[Out, MarshalAs ( UnmanagedType.LPStruct )]gme_info_t info_out,
			/*int*/				int track );

		[DllImport ( "libgme", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl )]
		/*void*/			private static extern void gme_free_info (
			/*gme_info_t***/	IntPtr info_out );

		[DllImport ( "libgme", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl )]
		/*void*/			private static extern void gme_enable_accuracy (
			/*Music_Emu**/		IntPtr emu,
			/*int*/				int enabled );

		private static string StringFromCharPtr ( IntPtr a_ptr )
		{
			if ( a_ptr != null )
			{
				return Marshal.PtrToStringAnsi ( a_ptr );
			}
			else
			{
				return null;
			}
		}

		#endregion

		public class UninitialisedException : Exception { }
		public class UnsupportedException : Exception { }
		public class LibException : Exception { public LibException ( string a_msg ) : base ( a_msg ) { } }

		public class VgmStream : Stream
		{
			#region Whatever
			// Will never change.
			public override bool CanRead => true;
			public override bool CanWrite => false;
			public override void SetLength ( long value ) { throw new NotSupportedException (); }
			public override void Write ( byte[] buffer, int offset, int count ) { throw new NotSupportedException (); }

			// Might be supported?
			public override void Flush () { throw new NotSupportedException (); }
			public override bool CanSeek => false;
			public override long Seek ( long offset, SeekOrigin origin ) { throw new NotSupportedException (); }
			public override long Length => throw new NotSupportedException ();
			public override long Position
			{
				get => throw new NotImplementedException ();
				set => throw new NotImplementedException ();
			}
			#endregion

			IntPtr m_emu = IntPtr.Zero;
			int m_iTell = 0;
			byte[] m_iBuf = new byte[2048];
			int m_iPos = 0;

			//public VgmStream ( byte[] a_data, int a_sampleRate = 48000 )
			public VgmStream ( string a_uri, int a_sampleRate = 48000 )
			{
				string err;

				/*
				err = StringFromCharPtr ( gme_identify_header ( a_data ) );
				if ( String.IsNullOrEmpty ( err ) )
				{
					throw new UnsupportedException ();
				}
				*/
				
				// Open data as a new emulator.
				/*
				err = StringFromCharPtr ( gme_open_data ( a_data, a_data.Length, out m_emu, a_sampleRate ) );
				if ( err != null )
				{
					throw new LibException ( $"gme error: {err}" );
				}
				*/

				err = StringFromCharPtr ( gme_open_file ( a_uri, out m_emu, a_sampleRate ) );
				if ( err != null )
				{
					throw new LibException ( $"gme error: {err}" );
				}

				// Check song count.
				int trackCount = gme_track_count ( m_emu );
				if ( trackCount <= 0 )
				{
					gme_delete ( m_emu );
					throw new LibException ( $"track count is {trackCount}" );
				}
				
				err = StringFromCharPtr ( gme_start_track ( m_emu, 0 ) );
				if ( err != null )
				{
					gme_delete ( m_emu );
					throw new LibException ( $"gme error: {err}" );
				}

				gme_enable_accuracy ( m_emu, 1 );

				m_iTell = 0;
				m_iPos = 0;
			}

			protected override void Dispose ( bool disposing )
			{
				if ( m_emu != IntPtr.Zero )
				{
					gme_delete ( m_emu );
					m_emu = IntPtr.Zero;
				}
			}

			private int ReadInternal ()
			{
				if ( gme_track_ended ( m_emu ) != 0 )
				{
					return 0;
				}

				string err = StringFromCharPtr ( gme_play ( m_emu, m_iBuf.Length / 2, m_iBuf ) );
				if ( err != null )
				{
					throw new LibException ( err );
				}

				//int prevTell = m_iTell;
				//m_iTell = gme_tell ( m_emu );
				//return ( m_iTell - prevTell ) * 4;
				return m_iBuf.Length;
			}

			public override int Read ( byte[] buffer, int offset, int count )
			{
				if ( m_emu == IntPtr.Zero )
				{
					throw new UninitialisedException ();
				}
				
				int processed = offset;
				int res = 0;
				while ( processed < count )
				{
					if ( m_iPos == 0 )
					{
						res = ReadInternal ();
					}
					else
					{
						res = m_iPos;
					}

					if ( res > 0 )
					{
						int copyLen = Math.Min ( res, count - processed ) - m_iPos;
						//copyLen = CopyEndianSwap16 ( ref m_iBuf, ref buffer, m_iPos, processed, copyLen / 2 );
						Array.Copy ( m_iBuf, m_iPos, buffer, processed, copyLen );
						processed += copyLen;

						if ( copyLen >= res )
						{
							m_iPos = 0;
						}
					}
					else
					{
						break;
					}
				}

				return processed - offset;
			}
		}
    }
}
