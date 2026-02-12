using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Media;
using System.Threading;
using MESharp.Services;
using Microsoft.Extensions.DependencyInjection;

namespace MESharp
{
	/// <summary>
	/// ScriptEntry for MESharp WPF Debug Utility.
	///
	/// REQUIRED FOR HOT-RELOAD:
	/// - public static class ScriptEntry in MESharp namespace
	/// - public static void Initialize()
	/// - public static void Shutdown()
	///
	/// OPTIONAL FEATURES USED IN THIS EXAMPLE:
	/// - Service configuration (DI container)
	/// - Custom WpfScriptShell service
	/// - Theme/resource management
	///
	/// For simpler scripts, see cli_template_minimal.txt or wpf_template_minimal.txt
	/// </summary>
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
		/// Initialize entry point for hot-reload path (called via reflection from ScriptLoader).
		/// This is the ONLY required method for hot-reload to work.
		/// </summary>
		public static void Initialize()
		{
			lock (_initLock)
			{
				if (_initialized)
				{
					Console.WriteLine("[Managed] Initialize() already executed; skipping duplicate call.");
					return;
				}

				// OPTIONAL: Configure services for dependency injection
				// This is NOT required for most scripts - only if you need DI container features
				if (!_servicesConfigured)
				{
					try
					{
						ScriptRuntime.ConfigureServices(services =>
						{
							// Register your services here
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
		/// This method is optional but HIGHLY RECOMMENDED for clean resource cleanup.
		/// </summary>
		public static void Shutdown()
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

            // Fallback path: explicitly request window close + dispatcher shutdown.
            // This guarantees unload works even if shutdown-token registration was skipped.
            var dispatcher = _uiDispatcher ?? _mainWindow?.Dispatcher;
            if (dispatcher != null)
            {
                try
                {
                    dispatcher.Invoke(() =>
                    {
                        try
                        {
                            _mainWindow?.Close();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("[Managed] Failed to close main window during shutdown: " + ex.Message);
                        }

                        if (!dispatcher.HasShutdownStarted)
                        {
                            dispatcher.InvokeShutdown();
                        }
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[Managed] Dispatcher shutdown invoke failed: " + ex.Message);
                }
            }

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
                    EnsureThemeResources(app);

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
                    EnsureThemeResources(app);
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
                        // OPTIONAL: Manual resource bootstrapping
                        // This is only needed if you have custom themes or MahApps.Metro resources
                        // Most scripts don't need this complexity
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
                _mainWindow = window;

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

        /// <summary>
        /// OPTIONAL: Manual resource bootstrapping for custom themes and MahApps.Metro.
        /// Most scripts don't need this - it's only here for the debug utility's fancy UI.
        /// For simpler scripts, just use plain WPF controls without custom themes.
        /// </summary>
        private static void BootstrapResources(Application app)
        {
            if (Application.ResourceAssembly == null)
            {
                Application.ResourceAssembly = Assembly.GetExecutingAssembly();
            }
            var assemblyName = Assembly.GetExecutingAssembly().GetName().Name ?? "MESharp_DebugUtil";

            var fallbackUris = new[]
            {
                $"pack://application:,,,/{assemblyName};component/Themes/Light.xaml",
                $"pack://application:,,,/{assemblyName};component/Themes/ItemFlagResources.xaml"
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

        private static void EnsureThemeResources(Application app)
        {
            try
            {
                if (!app.Resources.Contains("PrimaryCommandButton") || !app.Resources.Contains("SecondaryCommandButton"))
                {
                    Console.WriteLine("[Managed] Ensuring theme resources for reused Application.");
                    BootstrapResources(app);
                }
                TryApplyTheme();
            }
            catch (Exception ex)
            {
                Console.WriteLine("[Managed] EnsureThemeResources failed: " + ex.Message);
            }
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
                var targetUri = new Uri(uriString, UriKind.Absolute);
                foreach (var existing in app.Resources.MergedDictionaries)
                {
                    if (existing.Source == null)
                    {
                        continue;
                    }

                    if (Uri.Compare(existing.Source, targetUri, UriComponents.AbsoluteUri, UriFormat.Unescaped, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        return;
                    }
                }

                var dict = new ResourceDictionary { Source = targetUri };
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

        /// <summary>
        /// OPTIONAL: Custom service for managing WPF dispatcher across services.
        /// Most scripts don't need this - it's only here for advanced DI scenarios.
        /// </summary>
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
    /// <summary>
    /// OPTIONAL: Custom service for managing WPF dispatcher in DI scenarios.
    /// Most scripts don't need this - it's only here for the debug utility's DI setup.
    /// </summary>
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
