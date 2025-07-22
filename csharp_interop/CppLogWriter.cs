using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ME_csharp.API
{
	public class CppLogWriter : TextWriter
	{
		// The delegate that matches the C++ function's signature
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void LoggerCallback(string message);

		private readonly LoggerCallback _loggerCallback;

		public CppLogWriter(IntPtr loggerCallbackPtr)
		{
			// Convert the C++ function pointer to a callable C# delegate
			_loggerCallback = Marshal.GetDelegateForFunctionPointer<LoggerCallback>(loggerCallbackPtr);
		}

		// This is the most important method to override
		public override void WriteLine(string? value)
		{
			_loggerCallback?.Invoke(value ?? string.Empty);
		}

		// This is also required
		public override Encoding Encoding => Encoding.UTF8;
	}
}
