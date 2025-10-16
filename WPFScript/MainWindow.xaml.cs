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

namespace MESharp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
			public MainWindow()
			{
			InitializeComponent();

			// Apply saved theme once window resources are available
			try
			{
				var settings = MESharp.Services.ThemeManager.LoadSettings();
				MESharp.Services.ThemeManager.ApplyTheme(settings);
			}
			catch { /* ignore theme init issues */ }

            var vm = new MainWindowViewModel();
            	this.DataContext = vm;

			// Register our UI thread and hwnd with the native layer so it doesn't spoof focus
				try
                {
                    Console.WriteLine("[Managed] Registering UI thread + hwnd");
                    API.Focus.RegisterManagedThread(NativeMethods.GetCurrentThreadId());
					var hwnd = new WindowInteropHelper(this).Handle;
                    Console.WriteLine($"[Managed] MainWindow HWND={hwnd}");
				    API.Focus.RegisterManagedWindow(hwnd);

                    // Nudge focus towards this window when user clicks inside (helps fight native focus spoofing)
                    this.PreviewMouseDown += (_, __) =>
                    {
                        try { this.Activate(); Keyboard.Focus(this); } catch { }
                    };
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Managed] Failed to register UI thread/HWND: {ex}");
                }

            // Once the window is loaded and we have an HWND, ask native side to bring us to foreground
            this.Loaded += (_, __) =>
            {
                try
                {
                    Console.WriteLine("[Managed] Requesting native focus activation + spoof OFF");
					API.Focus.ActivateManagedWindow();
					API.Focus.SetFocusSpoofEnabled(false);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Managed] Activate request failed: {ex}");
                }
            };

            this.Activated += (_, __) =>
            {
                try
                {
                    Console.WriteLine("[Managed] Window activated; spoof OFF");
					API.Focus.SetFocusSpoofEnabled(false);
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
					API.Focus.SetFocusSpoofEnabled(true);
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
					API.Focus.SetFocusSpoofEnabled(true);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Managed] Close handler failed: {ex}");
                }
                finally
                {
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
    }

    internal static class NativeMethods
    {
        [DllImport("kernel32.dll")]
        internal static extern uint GetCurrentThreadId();
    }
}
