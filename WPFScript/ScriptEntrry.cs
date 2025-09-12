
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
            // Keep as a foreground thread so its dispatcher stays alive reliably
            uiThread.IsBackground = false;
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
            // Ensure Application exists (when launched from native host)
            var app = Application.Current ?? new Application();

            // Merge default theme so DynamicResource lookups have values before window loads
            try
            {
                string asmName = (Application.ResourceAssembly ?? Assembly.GetExecutingAssembly()).GetName().Name;
                Uri themeUri = new Uri($"pack://application:,,,/{asmName};component/Themes/Light.xaml", UriKind.Absolute);
                app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = themeUri });
            }
            catch { /* ignore theme init issues */ }

            // Run the dispatcher with our main window so it owns activation properly
            app.Run(new MainWindow());
        }

		//private static void InitAndShowWindow()
		//{
		//	// 1) Create the WPF Application if it doesn't already exist
		//	var app = Application.Current ?? new Application();

		//	// 2) Merge all of your MahApps + MaterialDesign resource dictionaries
		//	void AddDict(string packUri) =>
		//		app.Resources.MergedDictionaries.Add(new ResourceDictionary
		//		{
		//			Source = new Uri(packUri, UriKind.Absolute)
		//		});

		//	// MahApps.Metro
		//	AddDict("pack://application:,,,/MahApps.Metro;component/Styles/Controls.xaml");
		//	AddDict("pack://application:,,,/MahApps.Metro;component/Styles/Fonts.xaml");
		//	AddDict("pack://application:,,,/MahApps.Metro;component/Styles/Accents/Blue.xaml");
		//	AddDict("pack://application:,,,/MahApps.Metro;component/Styles/Accents/BaseDark.xaml");
		//	AddDict("pack://application:,,,/MahApps.Metro;component/Styles/Colors.xaml");

		//	// MaterialDesignThemes
		//	AddDict("pack://application:,,,/MaterialDesignThemes.Wpf;component/Themes/MaterialDesignTheme.Defaults.xaml");
		//	AddDict("pack://application:,,,/MaterialDesignThemes.Wpf;component/Themes/MaterialDesignTheme.Light.xaml");
		//	AddDict("pack://application:,,,/MaterialDesignThemes.Wpf;component/Themes/MaterialDesignTheme.Dark.xaml");

		//	// MaterialDesignColors (pick whatever palettes you want)
		//	AddDict("pack://application:,,,/MaterialDesignColors;component/Themes/Recommended/Primary/MaterialDesignColor.Blue.xaml");
		//	AddDict("pack://application:,,,/MaterialDesignColors;component/Themes/Recommended/Accent/MaterialDesignColor.Lime.xaml");

		//	// 3) Now you can safely show your MainWindow
		//	app.Run(new MainWindow());
		//}


	}

}
