using System.Windows;
using System.Windows.Controls;
using MESharp.ViewModels;

namespace MESharp.Views
{
    public partial class ThemeSettingsView : UserControl
    {
        private Window? _trackedWindow;

        public ThemeSettingsView()
        {
            InitializeComponent();

            // While this page is visible, manual window resizes show up live in the
            // size dropdown as "Custom (WxH)". Subscribed per-visit and detached on
            // unload so repeated visits don't stack handlers.
            Loaded += (_, __) =>
            {
                _trackedWindow = Window.GetWindow(this);
                if (_trackedWindow != null)
                    _trackedWindow.SizeChanged += OnWindowSizeChanged;
                (DataContext as ThemeSettingsViewModel)?.RefreshCurrentSize();
            };
            Unloaded += (_, __) =>
            {
                if (_trackedWindow != null)
                    _trackedWindow.SizeChanged -= OnWindowSizeChanged;
                _trackedWindow = null;
            };
        }

        private void OnWindowSizeChanged(object sender, SizeChangedEventArgs e)
            => (DataContext as ThemeSettingsViewModel)?.RefreshCurrentSize();
    }
}
