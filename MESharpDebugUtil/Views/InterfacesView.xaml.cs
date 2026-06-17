using System.Windows.Controls;
using System.Windows;
using MESharp.ViewModels;

namespace MESharp.Views
{
	public partial class InterfacesView : UserControl
	{
		public InterfacesView()
		{
			InitializeComponent();
		}

		private void OnSelectedInterfaceChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
		{
			if (DataContext is InterfacesViewModel viewModel &&
				e.NewValue is InterfaceComponentViewModel { IsLazyPlaceholder: false } selected)
			{
				viewModel.SelectedInterface = selected.Component;
			}
		}

		private void OnInterfaceTreeItemExpanded(object sender, RoutedEventArgs e)
		{
			if (!ReferenceEquals(sender, e.OriginalSource))
			{
				return;
			}

			if (DataContext is InterfacesViewModel viewModel &&
				sender is TreeViewItem { DataContext: InterfaceComponentViewModel node })
			{
				viewModel.EnsureChildrenLoaded(node);
			}
		}
	}
}
