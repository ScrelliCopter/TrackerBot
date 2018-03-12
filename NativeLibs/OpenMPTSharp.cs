using System;
using System.IO;
using System.Runtime.InteropServices;

namespace OpenMPTSharp
{
	public static class Modplug
	{
		#region DllImports
		
		private enum ErrorCode
		{
			OPENMPT_ERROR_OK	= 0,
			OPENMPT_ERROR_BASE	= 256,

			OPENMPT_ERROR_UNKNOWN					= OPENMPT_ERROR_BASE + 1,
			OPENMPT_ERROR_EXCEPTION					= OPENMPT_ERROR_BASE + 11,
			OPENMPT_ERROR_OUT_OF_MEMORY				= OPENMPT_ERROR_BASE + 21,
			OPENMPT_ERROR_RUNTIME					= OPENMPT_ERROR_BASE + 30,
			OPENMPT_ERROR_RANGE						= OPENMPT_ERROR_BASE + 31,
			OPENMPT_ERROR_OVERFLOW					= OPENMPT_ERROR_BASE + 32,
			OPENMPT_ERROR_UNDERFLOW					= OPENMPT_ERROR_BASE + 33,
			OPENMPT_ERROR_LOGIC						= OPENMPT_ERROR_BASE + 40,
			OPENMPT_ERROR_DOMAIN					= OPENMPT_ERROR_BASE + 41,
			OPENMPT_ERROR_LENGTH					= OPENMPT_ERROR_BASE + 42,
			OPENMPT_ERROR_OUT_OF_RANGE				= OPENMPT_ERROR_BASE + 43,
			OPENMPT_ERROR_INVALID_ARGUMENT			= OPENMPT_ERROR_BASE + 44,
			OPENMPT_ERROR_GENERAL					= OPENMPT_ERROR_BASE + 101,
			OPENMPT_ERROR_INVALID_MODULE_POINTER	= OPENMPT_ERROR_BASE + 102,
			OPENMPT_ERROR_ARGUMENT_NULL_POINTER		= OPENMPT_ERROR_BASE + 103,

			OPENMPT_ERROR_FUNC_RESULT_NONE		= 0,
			OPENMPT_ERROR_FUNC_RESULT_LOG		= 1 << 0,
			OPENMPT_ERROR_FUNC_RESULT_STORE		= 1 << 1,
			OPENMPT_ERROR_FUNC_RESULT_DEFAULT	= OPENMPT_ERROR_FUNC_RESULT_LOG | OPENMPT_ERROR_FUNC_RESULT_STORE
		}

		private enum RenderParam : int
		{
			OPENMPT_MODULE_RENDER_MASTERGAIN_MILLIBEL			= 1,
			OPENMPT_MODULE_RENDER_STEREOSEPARATION_PERCENT		= 2,
			OPENMPT_MODULE_RENDER_INTERPOLATIONFILTER_LENGTH	= 3,
			OPENMPT_MODULE_RENDER_VOLUMERAMPING_STRENGTH		= 4
		}

		[DllImport ( "libopenmpt", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl )]
		/*uint32_t*/							private static extern int openmpt_get_library_version ();

		[DllImport ( "libopenmpt", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl )]
		/*void*/								private static extern void openmpt_free_string (
			/*const char**/							IntPtr str );

		[DllImport ( "libopenmpt", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl )]
		/*const char**/							private static extern IntPtr openmpt_error_string (
			/*int*/									int error );

		[DllImport ( "libopenmpt", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl )]
		/*openmpt_module**/						private static extern IntPtr openmpt_module_create_from_memory2 (
			/*const void*/							byte[] filedata,
			/*size_t*/								UIntPtr filesize,
			/*openmpt_log_func*/					IntPtr logfunc,
			/*void**/								IntPtr loguser,
			/*openmpt_error_func*/					IntPtr errfunc,
			/*void**/								IntPtr erruser,
			/*int**/								[Out]int error,
			/*const char**/							[Out, MarshalAs(UnmanagedType.LPStr)]string error_message,
			/*const openmpt_module_inital_ctl**/	[Out]IntPtr ctls );

		[DllImport ( "libopenmpt", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl )]
		/*void*/								private static extern void openmpt_module_destroy (
			/*openmpt_module**/						IntPtr mod );

		[DllImport ( "libopenmpt", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl )]
		/*size_t*/								private static extern UIntPtr openmpt_module_read_interleaved_stereo (
			/*openmpt_module**/						IntPtr mod,
			/*int32_t*/								Int32 samplerate,
			/*size_t*/								UIntPtr count,
			/*int16_t**/							[Out]byte[] interleaved_stereo );

		[DllImport ( "libopenmpt", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl )]
		/*int*/									private static extern int openmpt_module_set_render_param (
			/*openmpt_module**/						IntPtr mod,
			/*int*/									RenderParam param,
			/*int32_t*/								Int32 value );

		#endregion

		public static int GetLibraryVersion ()
		{
			return openmpt_get_library_version ();
		}

		public class UninitialisedException : Exception { }
		public class LibException : Exception { public LibException ( string a_msg ) : base ( a_msg ) { } }

		public class ModStream : Stream
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

			IntPtr m_mod = IntPtr.Zero;
			int m_rate = 0;
			IntPtr m_ctls = IntPtr.Zero;

			public ModStream ( byte[] a_modData, int a_sampleRate = 48000 )
			{
				int error = 0;
				string errMsg = null;

				m_mod = openmpt_module_create_from_memory2 (
					a_modData, (UIntPtr)a_modData.Length,
					IntPtr.Zero, IntPtr.Zero,
					IntPtr.Zero, IntPtr.Zero,
					error, errMsg, m_ctls );

				if ( m_mod == IntPtr.Zero )
				{
					throw new LibException ( $"error code {error}: {errMsg}" );
				}

				m_rate = a_sampleRate;

				openmpt_module_set_render_param ( m_mod, RenderParam.OPENMPT_MODULE_RENDER_VOLUMERAMPING_STRENGTH, 0 );
				openmpt_module_set_render_param ( m_mod, RenderParam.OPENMPT_MODULE_RENDER_INTERPOLATIONFILTER_LENGTH, 1 );
			}

			protected override void Dispose ( bool disposing )
			{
				if ( m_mod != IntPtr.Zero )
				{
					openmpt_module_destroy ( m_mod );
					m_mod = IntPtr.Zero;
				}
			}

			public override int Read ( byte[] buffer, int offset, int count )
			{
				if ( m_mod == IntPtr.Zero )
				{
					throw new UninitialisedException ();
				}

				return (int)openmpt_module_read_interleaved_stereo ( m_mod, m_rate, (UIntPtr)(count / 4), buffer ) * 4;
			}
		}
	}
}
