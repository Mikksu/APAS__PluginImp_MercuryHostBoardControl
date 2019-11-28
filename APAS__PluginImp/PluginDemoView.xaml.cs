using System.Windows;
using System.Windows.Controls;

namespace MercuryHostBoard
{
    /// <summary>
    /// Interaction logic for AWGTesterView.xaml
    /// </summary>
    public partial class PluginDemoView : UserControl
    {
        public PluginDemoView()
        {
            InitializeComponent();
        }

        private async void BtnReconnect_Click(object sender, RoutedEventArgs e)
        {
            var backend = this.DataContext as PluginDemo;
            await backend.Control("RECONN");
        }
    }
}
