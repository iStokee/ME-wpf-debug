using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using MESharp.Services;

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
	/// - Theme/resource management
	///
	/// For simpler scripts, see cli_template_minimal.txt or wpf_template_minimal.txt
	/// </summary>
	public static class ScriptEntry
	{
        private static readonly object _initLock = new();
        private static bool _initialized;
        private static Window? _mainWindow;

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

				WpfScriptHost.Run(CreateMainWindow, new UiScriptHostOptions
				{
					ScriptName = "MESharp WPF Debug",
					ResourceAssembly = typeof(ScriptEntry).Assembly
				});

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

        Console.WriteLine("[Managed] Shutdown() invoked; closing WPF debug utility.");

        try
        {
            WpfScriptHost.Stop();
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

            _mainWindow = null;

            Console.WriteLine("[Managed] Shutdown cleanup complete; ready for hot reload.");
        }
    }

		private static Window CreateMainWindow()
	    {
            try
            {
                Console.WriteLine("[Managed] Creating WPF debug utility window on shared WPF host.");
                var app = Application.Current ?? throw new InvalidOperationException("WPF Application.Current is unavailable.");
                BootstrapResources(app);
                TryApplyTheme();

                Console.WriteLine("[Managed] Launching MainWindow");

                var window = new MainWindow();
                _mainWindow = window;
                return window;
            }
            catch (Exception ex)
            {
                Console.WriteLine("[Managed] CreateMainWindow failed: " + ex);

                lock (_initLock)
                {
                    _initialized = false;
                }

                throw;
            }
        }

        /// <summary>
        /// OPTIONAL: Manual resource bootstrapping for custom themes and MahApps.Metro.
        /// Most scripts don't need this - it's only here for the debug utility's fancy UI.
        /// For simpler scripts, just use plain WPF controls without custom themes.
        /// </summary>
        private static void BootstrapResources(Application app)
        {
            try
            {
                if (!ReferenceEquals(Application.ResourceAssembly, Assembly.GetExecutingAssembly()))
                {
                    Application.ResourceAssembly = Assembly.GetExecutingAssembly();
                }
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"[Managed] Application.ResourceAssembly already fixed as {Application.ResourceAssembly?.GetName().Name ?? "<null>"}; using explicit pack URIs. {ex.Message}");
            }

            var assemblyName = Assembly.GetExecutingAssembly().GetName().Name ?? "MESharp_DebugUtil";

            var mahAppsUris = new[]
            {
                "pack://application:,,,/MahApps.Metro;component/Styles/Controls.xaml",
                "pack://application:,,,/MahApps.Metro;component/Styles/Controls.Buttons.xaml",
                "pack://application:,,,/MahApps.Metro;component/Styles/Fonts.xaml"
            };

            foreach (var uri in mahAppsUris)
            {
                TryMergeDictionary(app, uri, swallowErrors: true);
            }

            // Keep the script theme after third-party dictionaries so its implicit
            // control styles win when Application resources are rebuilt on reload.
            var scriptThemeUris = new[]
            {
                $"pack://application:,,,/{assemblyName};component/Themes/Light.xaml",
                $"pack://application:,,,/{assemblyName};component/Themes/ItemFlagResources.xaml"
            };

            foreach (var uri in scriptThemeUris)
            {
                TryMergeDictionary(app, uri, swallowErrors: false);
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

    }

}
