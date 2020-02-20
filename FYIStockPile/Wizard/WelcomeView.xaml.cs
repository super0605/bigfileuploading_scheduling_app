using FYIStockPile.Storage;
using LiteDB;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using FYIStockPile.Interfaces;

namespace FYIStockPile.Wizard
{
    /// <summary>
    /// Interaction logic for WelcomeView.xaml
    /// </summary>
    public partial class WelcomeView : UserControl
    {
        private static LiteDatabase Database = new LiteDatabase(@"fyi_data.db");
        private static Slack slack = new Slack();
        private AppRepository Pile = new AppRepository(Database, slack);

        public WelcomeView()
        {
            InitializeComponent();
        }

        private void CheckProductkey(object sender, RoutedEventArgs e)  // unused function
        {
            string productKey = BucketName.Text;
            if(productKey == "")
            {
                handleMessageBox();
            }
        }

        private void handleMessageBox()
        {
            string messageBoxText = "Please add product key before clicking the Next button?";
            string caption = "FYIStockPile";
            MessageBoxButton button = MessageBoxButton.OKCancel;
            MessageBoxImage icon = MessageBoxImage.Warning;
            MessageBox.Show(messageBoxText, caption, button, icon);
        }

        private void HandleUseProxy(object sender, RoutedEventArgs e)
        {
            
            if (checkBoxUseProxy.IsChecked == true)
            {
                UseProxyPanel.Visibility = Visibility.Visible;
            } else
            {
                UseProxyPanel.Visibility = Visibility.Collapsed;
                textBoxHost.Text = "";
                textBoxPort.Text = "";
                textBoxUsername.Text = "";
                passwordBox.Password = "";
            }
            
        }
    }
}
