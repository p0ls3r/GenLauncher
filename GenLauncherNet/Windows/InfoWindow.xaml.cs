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
    public partial class InfoWindow : Window
    {
        private bool ContinueLaunch = true;

        public InfoWindow(string mainInfo, string modsInfo)
        {
            InitializeComponent();

            MainMessage.Text = mainInfo;
            ModsMessage.Text = modsInfo;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            ContinueLaunch = false;
            this.Hide();
        }

        public bool GetResult()
        {
            this.Close();
            return ContinueLaunch;
        }

        private void Continue_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
        }
    }
}
