
using System;
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
            try
            {
                await backend.Control("RECONN");
            }
            catch(Exception ex)
            {
                MessageBox.Show($"无法连接Host板，{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
