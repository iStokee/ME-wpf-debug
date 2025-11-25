using System;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using MESharp.ViewModels;
using MESharp.API;
using MESharp.ViewModels;

namespace MESharp
{
	/// <summary>
	/// MainWindow for MESharp WPF Debug Utility.
	///
	/// OPTIONAL FEATURES DEMONSTRATED:
	/// - Focus management (Focus.RegisterManagedWindow, Focus.SetFocusSpoofEnabled)
	/// - Orbit integration (docking script windows into Orbit management app)
	///
	/// Most scripts don't need these features! For simpler scripts, just create
	/// a basic Window with your UI controls.
	/// </summary>
    public partial class MainWindow
    {
		private Guid? _orbitSessionId;

			public MainWindow()
			{
			InitializeComponent();

            var vm = new MainWindowViewModel();
            	this.DataContext = vm;

				try
                {
                    Console.WriteLine("[Managed] Registering UI thread + hwnd");
				    MESharp.API.Focus.RegisterManagedThread(NativeMethods.GetCurrentThreadId());
					var hwnd = new WindowInteropHelper(this).Handle;
                    Console.WriteLine($"[Managed] MainWindow HWND={hwnd}");
				    MESharp.API.Focus.RegisterManagedWindow(hwnd);

                    this.PreviewMouseDown += (_, __) =>
                    {
                        try { this.Activate(); Keyboard.Focus(this); } catch { }
                    };
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Managed] Failed to register UI thread/HWND: {ex}");
                }

            this.Loaded += (_, __) =>
            {
				// OPTIONAL: Activate focus management for this window
				// Most scripts don't need this
                try
                {
                    Console.WriteLine("[Managed] Requesting native focus activation + spoof OFF");
					MESharp.API.Focus.ActivateManagedWindow();
					MESharp.API.Focus.SetFocusSpoofEnabled(false);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Managed] Activate request failed: {ex}");
                }

                // OPTIONAL: Try to dock into Orbit management app if available
                // Orbit is a separate app that can manage multiple script windows
                // Most scripts don't need this - it's only useful for complex multi-window setups
                try
                {
                    _orbitSessionId = TryDockWithOrbit(new WindowInteropHelper(this).Handle);
                    if (_orbitSessionId.HasValue)
                    {
                        Console.WriteLine($"[Managed] Docked into Orbit with session ID: {_orbitSessionId}");
                    }
                    else
                    {
                        Console.WriteLine("[Managed] Orbit not available, running standalone");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Managed] Failed to dock with Orbit: {ex.Message}");
                }
            };

            this.Activated += (_, __) =>
            {
                try
                {
                    Console.WriteLine("[Managed] Window activated; spoof OFF");
					MESharp.API.Focus.SetFocusSpoofEnabled(false);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Managed] Activate handler failed: {ex}");
                }
            };

            this.Deactivated += (_, __) =>
            {
                try
                {
                    Console.WriteLine("[Managed] Window deactivated; spoof ON");
					MESharp.API.Focus.SetFocusSpoofEnabled(true);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Managed] Deactivate handler failed: {ex}");
                }
            };

            this.Closed += (_, __) =>
            {
                try
                {
                    Console.WriteLine("[Managed] Window closed; spoof ON");
					MESharp.API.Focus.SetFocusSpoofEnabled(true);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Managed] Close handler failed: {ex}");
                }
                finally
                {
                    // Undock from Orbit if registered
                    if (_orbitSessionId.HasValue)
                    {
                        try
                        {
                            TryUndockFromOrbit(_orbitSessionId.Value);
                            Console.WriteLine($"[Managed] Undocked from Orbit");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[Managed] Failed to undock from Orbit: {ex.Message}");
                        }
                    }

                    if (this.DataContext is IDisposable disposableVm)
                    {
                        try { disposableVm.Dispose(); } catch { /* ignore */ }
                    }
                }
            };
        }

        private void OnTitleBarMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
            {
                if (e.ClickCount == 2)
                {
                    ToggleMaximizeRestore();
                }
                else
                {
                    try { this.DragMove(); } catch { }
                }
            }
        }

        private void OnMinimizeClick(object sender, System.Windows.RoutedEventArgs e)
            => this.WindowState = WindowState.Minimized;

        private void OnMaximizeClick(object sender, System.Windows.RoutedEventArgs e)
            => ToggleMaximizeRestore();

        private void OnCloseClick(object sender, System.Windows.RoutedEventArgs e)
            => this.Close();

        private void ToggleMaximizeRestore()
        {
            this.WindowState = (this.WindowState == WindowState.Maximized)
                ? WindowState.Normal
                : WindowState.Maximized;
        }

        /// <summary>
        /// Try to dock with Orbit if available (uses reflection to avoid hard dependency)
        /// </summary>
        private static Guid? TryDockWithOrbit(IntPtr hwnd)
        {
            try
            {
                // Try to load Orbit.dll from the same directory
                var orbitPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Orbit.dll");
                if (!System.IO.File.Exists(orbitPath))
                {
                    return null; // Orbit not available
                }

                var orbitAssembly = System.Reflection.Assembly.LoadFrom(orbitPath);
                var orbitApiType = orbitAssembly.GetType("Orbit.OrbitAPI");
                if (orbitApiType == null)
                {
                    return null;
                }

                // Check if Orbit is available
                var isAvailableMethod = orbitApiType.GetMethod("IsOrbitAvailable", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (isAvailableMethod == null)
                {
                    return null;
                }

                var isAvailable = (bool)(isAvailableMethod.Invoke(null, null) ?? false);
                if (!isAvailable)
                {
                    return null;
                }

                // Register the window
                var registerMethod = orbitApiType.GetMethod("RegisterScriptWindow", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (registerMethod == null)
                {
                    return null;
                }

                var result = registerMethod.Invoke(null, new object[] { hwnd, "MESharp Debug Util" });
                return result as Guid?;
            }
            catch
            {
                return null; // Orbit integration failed, continue standalone
            }
        }

        /// <summary>
        /// Try to undock from Orbit (uses reflection to avoid hard dependency)
        /// </summary>
        private static void TryUndockFromOrbit(Guid sessionId)
        {
            try
            {
                var orbitPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Orbit.dll");
                if (!System.IO.File.Exists(orbitPath))
                {
                    return;
                }

                var orbitAssembly = System.Reflection.Assembly.LoadFrom(orbitPath);
                var orbitApiType = orbitAssembly.GetType("Orbit.OrbitAPI");
                if (orbitApiType == null)
                {
                    return;
                }

                var unregisterMethod = orbitApiType.GetMethod("UnregisterScriptWindow", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (unregisterMethod == null)
                {
                    return;
                }

                unregisterMethod.Invoke(null, new object[] { sessionId });
            }
            catch
            {
                // Silently fail - Orbit integration is optional
            }
        }

    }

    internal static class NativeMethods
    {
        [DllImport("kernel32.dll")]
        internal static extern uint GetCurrentThreadId();
    }
}
