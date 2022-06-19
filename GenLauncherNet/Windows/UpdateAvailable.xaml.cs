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
using System.Windows.Shapes;

namespace GenLauncherNet
{
    /// <summary>
    /// Interaction logic for UpdateAvailable.xaml
    /// </summary>
    public partial class UpdateAvailable : Window
    {
        public UpdateAvailable(string VersionName)
        {
            InitializeComponent();

            InfoTextBox.Text = String.Format("New update {0} for GenLauncher is available to download!", VersionName);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
