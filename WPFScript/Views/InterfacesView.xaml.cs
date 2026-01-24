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
			if (DataContext is InterfacesViewModel viewModel && e.NewValue is InterfaceComponentViewModel selected)
			{
				viewModel.SelectedInterface = selected.Component;
			}
		}
	}
}
