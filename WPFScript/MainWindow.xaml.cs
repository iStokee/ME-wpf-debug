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
    public partial class MainWindow
    {
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
