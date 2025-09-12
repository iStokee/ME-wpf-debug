using MESharp.ViewModels;
using MESharp.Native;
using System.Windows.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

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
					NativeUI.UI_RegisterWpfThreadId(NativeUI.GetCurrentThreadId());
					var hwnd = new WindowInteropHelper(this).Handle;
					NativeUI.UI_RegisterWpfHwnd(hwnd);

                    // Nudge focus towards this window when user clicks inside (helps fight native focus spoofing)
                    this.PreviewMouseDown += (_, __) =>
                    {
                        try { this.Activate(); Keyboard.Focus(this); } catch { }
                    };
                }
                catch { /* best effort */ }

            // Once the window is loaded and we have an HWND, ask native side to bring us to foreground
            this.Loaded += (_, __) =>
            {
                try { NativeUI.UI_ActivateWpfWindow(); } catch { }
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
}
