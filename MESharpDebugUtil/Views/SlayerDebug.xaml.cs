
using System.Windows;
using System.Windows.Controls;
using MESharp.API;

namespace ME_wpf_debug
{
    public partial class SlayerDebug : Page
    {
        public SlayerDebug()
        {
            InitializeComponent();
        }

        private void GetSlayerTask_Click(object sender, RoutedEventArgs e)
        {
            var slayerTask = Interfaces.GetSlayerTask();
            if (slayerTask != null)
            {
                SlayerTaskText.Text = $"Slayer Task: {slayerTask.Count} {slayerTask.MonsterName}";
            }
            else
            {
                SlayerTaskText.Text = "Could not retrieve slayer task.";
            }
        }
    }
}
