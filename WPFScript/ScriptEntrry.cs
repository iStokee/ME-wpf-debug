
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Threading;

namespace MESharp
{
	// A custom TextWriter that redirects output to a C++ function pointer.
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

	public static class ScriptEntry
	{
		/// <summary>
		/// This method is called first by the C++ host to set up the console redirection.
		/// </summary>
		[UnmanagedCallersOnly]
		public static void SetLogger(IntPtr loggerCallbackPtr)
		{
			try
			{
				var writer = new CppLogWriter(loggerCallbackPtr);
				Console.SetOut(writer);
				Console.SetError(writer); // Also redirect error stream
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Failed to set logger: {ex.Message}");
			}
		}

		[UnmanagedCallersOnly]
		public static void Initialize()
		{
			// 1) Ensure the CLR will load assemblies from the script folder
			AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
			{
				var name = new AssemblyName(args.Name).Name + ".dll";

				// assume scriptFolder is where your script and csharp_interop live
				var exeFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
				var candidate = Path.Combine(exeFolder, name);

				return File.Exists(candidate)
					? Assembly.LoadFrom(candidate)
					: null;
			};

			// 2) Spin up the STA UI thread
			var uiThread = new Thread(InitAndShowWindow);
			uiThread.SetApartmentState(ApartmentState.STA);
			uiThread.IsBackground = true;
			uiThread.Start();
		}


		//public static void Main()
		//{

		//	// 2) Spin up the STA UI thread
		//	var uiThread = new Thread(InitAndShowWindow);
		//	uiThread.SetApartmentState(ApartmentState.STA);
		//	//uiThread.IsBackground = true;
		//	uiThread.Start();
		//}

		private static void InitAndShowWindow()
		{
			// You may also need to set the current directory so that
			// P/Invokes find their native .dlls:
			//var exeFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
			//Environment.CurrentDirectory = exeFolder;

			//var app = new Application();
			var wnd = new MainWindow();
			wnd.ShowDialog();
			Dispatcher.Run();
		}

	}

}
