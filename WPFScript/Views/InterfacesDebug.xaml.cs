
using System.Windows;
using System.Windows.Controls;
using MESharp.API;

namespace ME_wpf_debug
{
    public partial class InterfacesDebug : Page
    {
        public InterfacesDebug()
        {
            InitializeComponent();
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            InterfacesTreeView.ItemsSource = Interfaces.GetAll();
        }
    }
}
