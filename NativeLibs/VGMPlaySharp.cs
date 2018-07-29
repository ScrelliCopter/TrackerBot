using System;
using System.IO;
using System.Runtime.InteropServices;

namespace VGMPlaySharp
{
	public class VgmPlay
	{
		#region DllImports
		
		[DllImport ( "libvgmplay", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl )]
		private static extern string vgmplayGetError ();

		[DllImport ( "libvgmplay", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl )]
		private static extern IntPtr vgmplayOpenFile (
			[MarshalAs ( UnmanagedType.LPStr )] string filepath,
			UInt32 samplerate,
			UInt32 buffersize );

		[DllImport ( "libvgmplay", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl )]
		private static extern void vgmplayClose (
			IntPtr vgmstream );

		[DllImport ( "libvgmplay", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl )]
		private static extern UInt32 vgmplayRead16 (
			IntPtr vgmstream,
			[Out]byte[] outBuf,
			UInt32 outLen );

		#endregion

		public class UninitialisedException : Exception { }
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

			IntPtr m_stream = IntPtr.Zero;

			public VgmStream ( string a_filePath, int a_sampleRate = 48000, int a_bufferSize = 48000 )
			{
				m_stream = vgmplayOpenFile ( a_filePath, (uint)a_sampleRate, (uint)a_bufferSize );
				if ( m_stream == IntPtr.Zero )
				{
					throw new LibException ( vgmplayGetError () );
				}
			}

			protected override void Dispose ( bool disposing )
			{
				if ( m_stream != IntPtr.Zero )
				{
					vgmplayClose ( m_stream );
					m_stream = IntPtr.Zero;
				}
			}

			public override int Read ( byte[] buffer, int offset, int count )
			{
				if ( m_stream == IntPtr.Zero )
				{
					throw new UninitialisedException ();
				}

				return (int)vgmplayRead16 ( m_stream, buffer, (uint)count );
			}
		}
	}
}
