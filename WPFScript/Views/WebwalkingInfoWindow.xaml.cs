using System.Windows;

namespace MESharp.Views
{
    public partial class WebwalkingInfoWindow : Window
    {
        public WebwalkingInfoWindow()
        {
            InitializeComponent();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
