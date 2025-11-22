using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Media;
using System.Threading;
using MESharp.Services;
using Microsoft.Extensions.DependencyInjection;

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
        private static bool _servicesConfigured;
        private static Thread? _uiThread;
        private static Dispatcher? _uiDispatcher;
        private static Application? _app;
        private static Window? _mainWindow;
        private static WpfScriptShell? _shellInstance;
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

				// Configure services before any other initialization
				if (!_servicesConfigured)
				{
					try
					{
						ScriptRuntime.ConfigureServices(services =>
						{
							services.AddSingleton<WpfScriptShell>();
						});
						_servicesConfigured = true;
						Console.WriteLine("[Managed] Services configured successfully.");
					}
					catch (InvalidOperationException ex)
					{
						Console.WriteLine($"[Managed] Service configuration skipped (provider already built): {ex.Message}");
						// Provider already exists; try to reuse any existing shell instance or fall back later.
						try
						{
							_shellInstance ??= ScriptRuntime.Services.GetService<WpfScriptShell>();
						}
						catch (Exception resolveEx)
						{
							Console.WriteLine($"[Managed] Could not resolve WpfScriptShell from existing provider: {resolveEx.Message}");
						}
					}
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

            // CRITICAL: DON'T call Application.Current.Shutdown() here!
            // We need to preserve Application.Current for hot reload.
            // The window closing naturally will exit app.Run() without destroying Application.Current.

            // Just wait for the UI thread to exit naturally (window already closed by user or shutdown signal)
            var uiThread = _uiThread;
            if (uiThread != null && uiThread.IsAlive)
            {
                Console.WriteLine("[Managed] Waiting for UI thread to exit naturally...");
                if (!uiThread.Join(TimeSpan.FromSeconds(5)))
                {
                    Console.WriteLine("[Managed] UI thread did not exit within timeout during shutdown.");
                }
                else
                {
                    Console.WriteLine("[Managed] UI thread exited successfully.");
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

            // CRITICAL: DON'T clear _app - preserve Application.Current for hot reload!
            // WPF only allows ONE Application instance per AppDomain.
            // Each reload creates a new thread with its own Dispatcher, but reuses Application.Current.

            lock (_initLock)
            {
                _initialized = false;
            }

            // Clear thread references (new thread will be created on next load)
            _mainWindow = null;
            _uiThread = null;
            _uiDispatcher = null; // New Dispatcher will be created on next thread
            _uiReady.Reset();

            Console.WriteLine("[Managed] Shutdown cleanup complete; ready for hot reload (Application.Current preserved)");
        }
    }

		private static void InitAndShowWindow()
	    {
            try
            {
                Console.WriteLine("[Managed] InitAndShowWindow starting on thread " + Thread.CurrentThread.ManagedThreadId);
                _uiDispatcher = Dispatcher.CurrentDispatcher;
                _uiReady.Set();

                try
                {
                    var shell = GetShell();
                    shell.RegisterDispatcher(_uiDispatcher);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[Managed] Failed to publish dispatcher to service provider: " + ex.Message);
                }

                // CRITICAL: WPF only allows ONE Application instance per AppDomain, EVER.
                // Even after Shutdown(), you cannot create a new Application() in the same AppDomain.
                // We must check Application.Current FIRST (AppDomain-wide singleton) to handle
                // the case where a different script was loaded in a new ALC.
                Application app;
                var initialized = false;

                // PRIORITY 1: Check AppDomain-wide Application.Current (persists across ALC loads)
                if (Application.Current != null)
                {
                    Console.WriteLine("[Managed] Reusing Application.Current from AppDomain (survives ALC unload)");
                    app = Application.Current;
                    _app = app; // Update static field for this ALC
                    initialized = true;

                    // Re-register shutdown handler with the NEW ShutdownMonitor token
                    // (the old registration is tied to the old, disposed token)
                    try
                    {
                        if (app is App appInstance)
                        {
                            App.DisposeShutdownRegistration();
                            Console.WriteLine("[Managed] Re-registering shutdown handler for reused Application");
                            appInstance.RegisterShutdownHandler();
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Managed] Failed to re-register shutdown handler: {ex.Message}");
                    }
                }
                // PRIORITY 2: Check static field from THIS ALC (only valid for same script reload)
                else if (_app != null)
                {
                    Console.WriteLine("[Managed] Reusing existing Application from static field (same ALC)");
                    app = _app;
                    initialized = true;
                }
                // PRIORITY 3: Create new instance (first load ever in this AppDomain)
                else
                {
                    Console.WriteLine("[Managed] Creating new App instance (first load in AppDomain)");
                    app = new App();
                    _app = app;

                    // CRITICAL: Prevent automatic shutdown when main window closes!
                    // This preserves Application.Current for hot reload.
                    // Must be set BEFORE any windows are created and on the same thread.
                    app.ShutdownMode = ShutdownMode.OnExplicitShutdown;

                    initialized = TryInitializeComponent((App)app);

                    if (!initialized)
                    {
                        Console.WriteLine("[Managed] Falling back to manual resource bootstrap.");
                        BootstrapResources(app);
                        TryApplyTheme();
                    }
                }

                app.DispatcherUnhandledException += (s, e) =>
                {
                    Console.WriteLine("[Managed] DispatcherUnhandledException: " + e.Exception);
                    e.Handled = true;
                };

                Console.WriteLine("[Managed] Launching MainWindow");

                // Create and show the window
                var window = new MainWindow();

                // When reusing Application.Current from a previous load, we need to manually show the window
                // and pump messages, because app.Run() can only be called once per Application instance.
                if (initialized)
                {
                    Console.WriteLine("[Managed] Reused Application - showing window and pumping dispatcher");
                    window.Show();
                    Dispatcher.Run(); // Pump messages on this thread until Dispatcher.InvokeShutdown() is called
                    Console.WriteLine("[Managed] Dispatcher.Run exited");
                }
                else
                {
                    // New Application - call Run() normally
                    Console.WriteLine("[Managed] New Application - calling app.Run()");
                    app.Run(window);
                    Console.WriteLine("[Managed] MainWindow.Run exited");
                }

                // DON'T reset state here - Shutdown() will handle cleanup
                // This prevents race condition where user closes window before hot reload unloads
                Console.WriteLine("[Managed] UI thread exiting naturally (window closed by user)");
            }
            catch (Exception ex)
            {
                Console.WriteLine("[Managed] InitAndShowWindow failed: " + ex);

                // On error, ensure we clean up
                lock (_initLock)
                {
                    _initialized = false;
                }
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

        private static WpfScriptShell GetShell()
        {
            // Prefer the DI container if the service was registered before the provider was built.
            try
            {
                var fromServices = ScriptRuntime.Services.GetService<WpfScriptShell>();
                if (fromServices != null)
                {
                    _shellInstance = fromServices;
                    return fromServices;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[Managed] ScriptRuntime.Services unavailable while resolving WpfScriptShell: " + ex.Message);
            }

            // Fallback: create a local instance so dispatcher registration always succeeds.
            _shellInstance ??= new WpfScriptShell();
            Console.WriteLine("[Managed] Using local WpfScriptShell instance (not registered in DI).");
            return _shellInstance;
        }

    }

}

namespace MESharp
{
    internal sealed class WpfScriptShell
    {
        private readonly object _sync = new();

        public Dispatcher? Dispatcher { get; private set; }

        public void RegisterDispatcher(Dispatcher dispatcher)
        {
            if (dispatcher == null) throw new ArgumentNullException(nameof(dispatcher));

            lock (_sync)
            {
                Dispatcher = dispatcher;
            }
        }
    }
}
