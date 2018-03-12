using System;
using System.IO;
using System.Runtime.InteropServices;

namespace HivelySharp
{
	public static class Hively
	{
		#region DllImports

		[StructLayout ( LayoutKind.Explicit, CharSet = CharSet.Ansi )]
		private struct hvl_tune
		{
			[FieldOffset ( 300 )]
			public Byte ht_SongEndReached;

			/*
			[MarshalAs ( UnmanagedType.ByValArray, SizeConst = 128 )]
			public char[]	ht_name;
			public UInt16	ht_SongNum;
			public UInt32	ht_Frequency;
			public Double	ht_FreqF;
			public IntPtr	ht_WaveformTab;
			public UInt16	ht_Restart;
			public UInt16	ht_PositionNr;
			public Byte		ht_SpeedMultiplier;
			public Byte		ht_TrackLength;
			public Byte		ht_TrackNr;
			public Byte		ht_InstrumentNr;
			public Byte		ht_SubsongNr;
			public UInt16	ht_PosJump;
			public UInt32	ht_PlayingTime;
			public Int16	ht_Tempo;
			public Int16	ht_PosNr;
			public Int16	ht_StepWaitFrames;
			public Int16	ht_NoteNr;
			public UInt16	ht_PosJumpNote;
			public Byte		ht_GetNewPosition;
			public Byte		ht_PatternBreak;
			public Byte		ht_SongEndReached;
			public Byte		ht_Stereo;
			public IntPtr	ht_Subsongs;
			public UInt16	ht_Channels;
			public IntPtr	ht_Positions;
			public IntPtr	ht_Tracks;
			public IntPtr	ht_Instruments;
			public IntPtr	ht_Voices;
			public IntPtr	ht_BlipBuffers;
			public Int32	ht_defstereo;
			public Int32	ht_defpanleft;
			public Int32	ht_defpanright;
			public Int32	ht_mixgain;
			public Byte		ht_Version;
			*/
		}

		[DllImport ( "hvl_replay", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl )]
		private static extern void hvl_DecodeFrame (
			IntPtr ht,
			[Out]byte[] buf1,
			[Out]byte[] buf2,
			Int32 bufmod );

		[DllImport ( "hvl_replay", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl )]
		private static extern void hvl_InitReplayer ();

		
		[DllImport ( "hvl_replay", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl )]
		private static extern bool hvl_InitSubsong (
			IntPtr ht,
			UInt32 nr );

		[DllImport ( "hvl_replay", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl )]
		private static extern IntPtr hvl_LoadTune (
			[MarshalAs ( UnmanagedType.LPStr )]string name,
			UInt32 freq,
			UInt32 defstereo );

		[DllImport ( "hvl_replay", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl )]
		private static extern void hvl_FreeTune (
			IntPtr ht );

		#endregion

		public static bool _initialised = false;

		public class OpenException : Exception { }

		public class HvlStream : Stream
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
			
			IntPtr m_tune;
			int m_bufLen;
			byte[] m_buf1 = null;
			byte[] m_buf2 = null;
			int m_rate;
			int m_bufPos;

			public HvlStream ( string a_filePath, int a_sampleRate = 48000 )
			{
				if ( !_initialised )
				{
					hvl_InitReplayer ();
					_initialised = true;
				}

				m_tune = hvl_LoadTune ( a_filePath, (uint)a_sampleRate, 2 );
				if ( m_tune == IntPtr.Zero )
				{
					throw new OpenException ();
				}

				m_bufLen = ( a_sampleRate * 2 ) / 50;
				m_buf1 = new byte[m_bufLen];
				m_buf2 = new byte[m_bufLen];
				m_rate = a_sampleRate;
				m_bufPos = 0;

				FillBuffer ();
			}

			protected override void Dispose ( bool disposing )
			{
				if ( m_tune != IntPtr.Zero )
				{
					hvl_FreeTune ( m_tune );
					m_tune = IntPtr.Zero;
				}
			}

			private void FillBuffer ()
			{
				hvl_DecodeFrame ( m_tune, m_buf1, m_buf2, 2 );
			}

			public override int Read ( byte[] buffer, int offset, int count )
			{
				if ( m_tune == IntPtr.Zero )
				{
					return 0;
				}

				var tuneStruct = Marshal.PtrToStructure<hvl_tune> ( m_tune );
				if ( tuneStruct.ht_SongEndReached != 0 )
				{
					return 0;
				}

				int samples = Math.Min ( m_bufLen / 2, count / 4 ) - m_bufPos / 4;
				int i;
				for ( i = 0; i < samples; ++i )
				{
					buffer[offset + i * 4 + 0] = m_buf1[m_bufPos];
					buffer[offset + i * 4 + 1] = m_buf1[m_bufPos + 1];
					buffer[offset + i * 4 + 2] = m_buf2[m_bufPos];
					buffer[offset + i * 4 + 3] = m_buf2[m_bufPos + 1];

					m_bufPos += 2;
				}

				if ( m_bufPos >= m_bufLen )
				{
					m_bufPos = 0;
					FillBuffer ();
				}

				return i * 4;
			}
		}
	}
}
