using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Media;
using System.Threading;

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
        private static readonly object _initLock = new();
        private static bool _initialized;
        private static Thread? _uiThread;
        private static Dispatcher? _uiDispatcher;
        private static readonly ManualResetEventSlim _uiReady = new(false);

		/// <summary>
		/// This method is called first by the C++ host to set up the console redirection.
		/// Legacy path only - uses native function pointer.
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

			/// <summary>
			/// Initialize entry point for hot-reload path (called via reflection from ScriptLoader).
			/// </summary>
			public static void Initialize()
			{
				Initialize_Impl();
			}

			/// <summary>
			/// Initialize entry point for legacy native path (called via function pointer from ME).
			/// </summary>
			[UnmanagedCallersOnly]
			public static void Initialize_Native()
			{
				Initialize_Impl();
			}

			/// <summary>
			/// Shared initialization logic for both legacy and hot-reload paths.
			/// </summary>
			private static void Initialize_Impl()
			{
            lock (_initLock)
            {
                if (_initialized)
                {
                    Console.WriteLine("[Managed] Initialize() already executed; skipping duplicate call.");
                    return;
                }

                _uiReady.Reset();

                // Spin up the STA UI thread
                _uiThread = new Thread(InitAndShowWindow)
                {
                    IsBackground = true,
                    Name = "MESharp.WpfUiThread"
                };
                _uiThread.SetApartmentState(ApartmentState.STA);
                _uiThread.Start();

                _initialized = true;
            }
			}

			/// <summary>
			/// Shutdown entry point for hot-reload path (called via reflection from ScriptLoader).
			/// </summary>
			public static void Shutdown()
			{
				Shutdown_Impl();
			}

			/// <summary>
			/// Shutdown entry point for legacy native path (called via function pointer from ME).
			/// </summary>
			[UnmanagedCallersOnly]
			public static void Shutdown_Native()
			{
				Shutdown_Impl();
			}

			/// <summary>
			/// Shared shutdown logic for both legacy and hot-reload paths.
			/// </summary>
			private static void Shutdown_Impl()
        {
            bool wasInitialized;
            lock (_initLock)
            {
                wasInitialized = _initialized;
            }

            if (!wasInitialized)
            {
                Console.WriteLine("[Managed] Shutdown() requested but Initialize() never completed.");
                return;
            }

            Console.WriteLine("[Managed] Shutdown() invoked; beginning WPF teardown.");

            try
            {
                if (!_uiReady.Wait(TimeSpan.FromSeconds(5)))
                {
                    Console.WriteLine("[Managed] Shutdown waiting for UI dispatcher timed out; continuing cleanup.");
                }

                var dispatcher = _uiDispatcher;
                if (dispatcher != null && !dispatcher.HasShutdownStarted && !dispatcher.HasShutdownFinished)
                {
                    dispatcher.InvokeAsync(() =>
                    {
                        Console.WriteLine("[Managed] Dispatcher shutting down Application.");
                        Application.Current?.Shutdown();
                    });
                }

                var uiThread = _uiThread;
                if (uiThread != null && uiThread.IsAlive)
                {
                    if (!uiThread.Join(TimeSpan.FromSeconds(5)))
                    {
                        Console.WriteLine("[Managed] UI thread did not exit within timeout during shutdown.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[Managed] Shutdown encountered an error: " + ex);
            }
            finally
            {
                App.DisposeShutdownRegistration();

                lock (_initLock)
                {
                    _initialized = false;
                }

                _uiDispatcher = null;
                _uiThread = null;
                _uiReady.Reset();
            }
        }


		//public static void Main()
		//{

		//	// 2) Spin up the STA UI thread
		//	var uiThread = new Thread(InitAndShowWindow);
		//	uiThread.SetApartmentState(ApartmentState.STA);
		//	//uiThread.IsBackground = true;
		//	uiThread.Start();
		//}

		//private static void InitAndShowWindow()
		//{
		//	// You may also need to set the current directory so that
		//	// P/Invokes find their native .dlls:
		//	//var exeFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
		//	//Environment.CurrentDirectory = exeFolder;

		//	//var app = new Application();
		//	var wnd = new MainWindow();
		//	wnd.ShowDialog();
		//	Dispatcher.Run();
		//}

		private static void InitAndShowWindow()
	    {
            try
            {
                Console.WriteLine("[Managed] InitAndShowWindow starting on thread " + Thread.CurrentThread.ManagedThreadId);
                _uiDispatcher = Dispatcher.CurrentDispatcher;
                _uiReady.Set();

                var app = Application.Current as App;
                var initialized = false;

                if (app == null)
                {
                    app = new App();
                    initialized = TryInitializeComponent(app);
                }
                else
                {
                    initialized = true;
                }

                if (!initialized)
                {
                    Console.WriteLine("[Managed] Falling back to manual resource bootstrap.");
                    BootstrapResources(app);
                    TryApplyTheme();
                }

                app.DispatcherUnhandledException += (s, e) =>
                {
                    Console.WriteLine("[Managed] DispatcherUnhandledException: " + e.Exception);
                    e.Handled = true;
                };

                Console.WriteLine("[Managed] Launching MainWindow");
                app.Run(new MainWindow());
                Console.WriteLine("[Managed] MainWindow.Run exited");

                App.DisposeShutdownRegistration();
                lock (_initLock)
                {
                    _initialized = false;
                }
                _uiDispatcher = null;
                _uiThread = null;
                _uiReady.Reset();
            }
            catch (Exception ex)
            {
                Console.WriteLine("[Managed] InitAndShowWindow failed: " + ex);
            }
        }

        private static bool TryInitializeComponent(App app)
        {
            try
            {
                app.InitializeComponent();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("[Managed] App.InitializeComponent failed: " + ex);
                return false;
            }
        }

        private static void BootstrapResources(Application app)
        {
            if (Application.ResourceAssembly == null)
            {
                Application.ResourceAssembly = Assembly.GetExecutingAssembly();
            }

            app.Resources.MergedDictionaries.Clear();

            var fallbackUris = new[]
            {
                "pack://application:,,,/WPFScript;component/Themes/Light.xaml",
                "pack://application:,,,/WPFScript;component/Themes/ItemFlagResources.xaml"
            };

            foreach (var uri in fallbackUris)
            {
                TryMergeDictionary(app, uri, swallowErrors: false);
            }

            var mahAppsUris = new[]
            {
                "pack://application:,,,/MahApps.Metro;component/Styles/Controls.xaml",
                "pack://application:,,,/MahApps.Metro;component/Styles/Controls.Buttons.xaml",
                "pack://application:,,,/MahApps.Metro;component/Styles/Fonts.xaml",
                "pack://application:,,,/MahApps.Metro;component/Styles/Colors.xaml",
                "pack://application:,,,/MahApps.Metro;component/Styles/Accents/Blue.xaml",
                "pack://application:,,,/MahApps.Metro;component/Styles/Accents/BaseLight.xaml"
            };

            foreach (var uri in mahAppsUris)
            {
                TryMergeDictionary(app, uri, swallowErrors: true);
            }

            var acrylicColor = Color.FromArgb(0xCC, 0xFF, 0xFF, 0xFF);
            EnsureResource(app, "AcrylicBackgroundColor", () => acrylicColor);
            EnsureResource(app, "AcrylicBackgroundBrush", () => CreateFrozenBrush(acrylicColor));

            var primaryColor = Color.FromArgb(0xFF, 0x3F, 0x51, 0xB5);
            EnsureResource(app, "PrimaryColor", () => primaryColor);
            EnsureResource(app, "PrimaryBrush", () => CreateFrozenBrush(primaryColor));
            EnsureResource(app, "PrimaryForegroundBrush", () => CreateFrozenBrush(Colors.White));
            EnsureResource(app, "PrimarySoftBrush", () => CreateFrozenBrush(Color.FromArgb(0x33, 0x3F, 0x51, 0xB5)));
        }

        private static SolidColorBrush CreateFrozenBrush(Color color)
        {
            var brush = new SolidColorBrush(color);
            if (brush.CanFreeze)
            {
                brush.Freeze();
            }
            return brush;
        }

        private static void EnsureResource(Application app, object key, Func<object> factory)
        {
            if (!app.Resources.Contains(key))
            {
                app.Resources[key] = factory();
            }
        }

        private static void TryMergeDictionary(Application app, string uriString, bool swallowErrors)
        {
            try
            {
                var dict = new ResourceDictionary { Source = new Uri(uriString, UriKind.Absolute) };
                app.Resources.MergedDictionaries.Add(dict);
            }
            catch (Exception ex)
            {
                if (!swallowErrors)
                {
                    throw;
                }

                Console.WriteLine($"[Managed] Skipped resource '{uriString}': {ex.Message}");
            }
        }

        private static void TryApplyTheme()
        {
            try
            {
                var settings = MESharp.Services.ThemeManager.LoadSettings();
                MESharp.Services.ThemeManager.ApplyTheme(settings);
            }
            catch (Exception ex)
            {
                Console.WriteLine("[Managed] Theme apply during bootstrap failed: " + ex);
            }
        }

    }

}
