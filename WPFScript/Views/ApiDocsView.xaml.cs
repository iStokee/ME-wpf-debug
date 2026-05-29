using MESharp.ViewModels;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace MESharp.Views
{
    public partial class ApiDocsView : UserControl
    {
        private ApiDocsViewModel? _viewModel;

        public ApiDocsView()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
            Unloaded += OnUnloaded;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            }

            _viewModel = DataContext as ApiDocsViewModel;
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged += OnViewModelPropertyChanged;
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
                _viewModel = null;
            }
        }

        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ApiDocsViewModel.SelectedClassDocumentation))
            {
                Dispatcher.BeginInvoke(new Action(() => DocumentationScrollViewer.ScrollToHome()), DispatcherPriority.Background);
            }
        }

        private void OnMemberSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is not ListBox listBox || e.AddedItems.Count == 0)
            {
                return;
            }

            var selectedItem = e.AddedItems[0];
            listBox.ScrollIntoView(selectedItem);
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (listBox.ItemContainerGenerator.ContainerFromItem(selectedItem) is FrameworkElement container)
                {
                    container.BringIntoView();
                }
            }), DispatcherPriority.Background);
        }
    }
}
