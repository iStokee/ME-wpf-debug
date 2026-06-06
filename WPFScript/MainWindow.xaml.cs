using System;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;

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
        private IntPtr _windowHandle = IntPtr.Zero;
        private bool _nativeWindowRegistered;
        private static readonly bool EnableOrbitDocking =
            string.Equals(Environment.GetEnvironmentVariable("MESHARP_DEBUGUTIL_DOCK_WITH_ORBIT"), "1", StringComparison.OrdinalIgnoreCase);

			public MainWindow()
			{
			InitializeComponent();

            var vm = new MainWindowViewModel();
            	this.DataContext = vm;

                try
                {
                    Console.WriteLine("[Managed] Registering UI thread");
                    MESharp.API.Focus.RegisterManagedThread(NativeMethods.GetCurrentThreadId());
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Managed] Failed to register UI thread: {ex}");
                }

                this.SourceInitialized += (_, __) =>
                {
                    TryRegisterNativeWindowHandle();
                };

                this.PreviewMouseDown += (_, e) =>
                {
                    try
                    {
                        if (ShouldForceWindowFocus(e.OriginalSource as DependencyObject))
                        {
                            this.Activate();
                            Keyboard.Focus(this);
                        }
                    }
                    catch { }
                };

            this.Loaded += (_, __) =>
            {
				// OPTIONAL: Activate focus management for this window
				// Most scripts don't need this
                try
                {
                    TryRegisterNativeWindowHandle();
                    Console.WriteLine("[Managed] Requesting native focus activation + spoof OFF");
					MESharp.API.Focus.ActivateManagedWindow();
					MESharp.API.Focus.SetFocusSpoofEnabled(false);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Managed] Activate request failed: {ex}");
                }

                if (EnableOrbitDocking)
                {
                    // OPTIONAL: Try to dock into Orbit management app if available
                    try
                    {
                        _orbitSessionId = TryDockWithOrbit(new WindowInteropHelper(this).Handle);
                        if (_orbitSessionId.HasValue)
                        {
                            Console.WriteLine($"[Managed] Docked into Orbit with session ID: {_orbitSessionId}");
                        }
                        else
                        {
                            Console.WriteLine("[Managed] Orbit docking enabled, but Orbit bridge was unavailable.");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Managed] Failed to dock with Orbit: {ex.Message}");
                    }
                }
                else
                {
                    Console.WriteLine("[Managed] Orbit auto-docking disabled for debug util (standalone window mode).");
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

        private static bool ShouldForceWindowFocus(DependencyObject? source)
        {
            var current = source;
            while (current != null)
            {
                if (current is ComboBox ||
                    current is ComboBoxItem ||
                    current is TextBoxBase ||
                    current is Selector ||
                    current is ButtonBase ||
                    current is PasswordBox ||
                    current is ListBoxItem ||
                    current is ListViewItem)
                {
                    return false;
                }

                current = VisualTreeHelper.GetParent(current);
            }

            return true;
        }

        private void TryRegisterNativeWindowHandle()
        {
            if (_nativeWindowRegistered)
            {
                return;
            }

            try
            {
                _windowHandle = new WindowInteropHelper(this).Handle;
                if (_windowHandle == IntPtr.Zero)
                {
                    Console.WriteLine("[Managed] Native HWND is not ready yet; postponing registration.");
                    return;
                }

                MESharp.API.Focus.RegisterManagedWindow(_windowHandle);
                _nativeWindowRegistered = true;
                Console.WriteLine($"[Managed] Registered UI HWND={_windowHandle}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Managed] Failed to register UI HWND: {ex.Message}");
            }
        }

        /// <summary>
        /// Try to dock with Orbit if available (uses reflection to avoid hard dependency)
        /// </summary>
        private static Guid? TryDockWithOrbit(IntPtr hwnd)
        {
            // Preferred path: cross-process Orbit API bridge
            var bridgeResponse = SendOrbitBridgeRequest(new
            {
                action = "register",
                windowHandle = hwnd.ToInt64().ToString(),
                tabName = "MESharp Debug Util",
                processId = Environment.ProcessId
            });
            if (bridgeResponse.ok && Guid.TryParse(bridgeResponse.sessionId, out var bridgedSessionId))
            {
                return bridgedSessionId;
            }

            try
            {
                // Try to load Orbit.dll from the same directory
                var orbitPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Orbit.dll");
                if (!System.IO.File.Exists(orbitPath))
                {
                    return null; // Orbit not available
                }

                var orbitAssembly = System.Reflection.Assembly.LoadFrom(orbitPath);
                var orbitApiType = orbitAssembly.GetType("Orbit.API.OrbitAPI");
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

                var result = registerMethod.Invoke(null, new object[] { hwnd, "MESharp Debug Util", Environment.ProcessId });
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
            var bridgeResponse = SendOrbitBridgeRequest(new
            {
                action = "unregister",
                sessionId = sessionId.ToString()
            });
            if (bridgeResponse.ok)
            {
                return;
            }

            try
            {
                var orbitPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Orbit.dll");
                if (!System.IO.File.Exists(orbitPath))
                {
                    return;
                }

                var orbitAssembly = System.Reflection.Assembly.LoadFrom(orbitPath);
                var orbitApiType = orbitAssembly.GetType("Orbit.API.OrbitAPI");
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

        private static OrbitBridgeResponse SendOrbitBridgeRequest(object payload)
        {
            try
            {
                using var pipe = new NamedPipeClientStream(".", "OrbitApiBridge", PipeDirection.InOut);
                pipe.Connect(500);

                using var writer = new StreamWriter(pipe, Encoding.UTF8, bufferSize: 4096, leaveOpen: true) { AutoFlush = true };
                using var reader = new StreamReader(pipe, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, bufferSize: 4096, leaveOpen: true);

                writer.WriteLine(JsonSerializer.Serialize(payload));
                var responseLine = reader.ReadLine();
                if (string.IsNullOrWhiteSpace(responseLine))
                {
                    return OrbitBridgeResponse.Failed("No response from Orbit bridge.");
                }

                return JsonSerializer.Deserialize<OrbitBridgeResponse>(responseLine) ?? OrbitBridgeResponse.Failed("Invalid bridge response.");
            }
            catch (Exception ex)
            {
                return OrbitBridgeResponse.Failed(ex.Message);
            }
        }

        private sealed class OrbitBridgeResponse
        {
            public bool ok { get; set; }
            public string? message { get; set; }
            public string? sessionId { get; set; }

            public static OrbitBridgeResponse Failed(string message) => new() { ok = false, message = message };
        }

    }

    internal static class NativeMethods
    {
        [DllImport("kernel32.dll")]
        internal static extern uint GetCurrentThreadId();
    }
}
