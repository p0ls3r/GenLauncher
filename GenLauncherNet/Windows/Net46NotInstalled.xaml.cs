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
    /// Interaction logic for Net46NotInstalled.xaml
    /// </summary>
    public partial class Net46NotInstalled : Window
    {
        public Net46NotInstalled()
        {
            InitializeComponent();
        }

        private void Net46LinkOpen(object sender, MouseButtonEventArgs e)
        {
            System.Diagnostics.Process.Start(Net46Link.Text);
        }

        private void Net48LinkOpen(object sender, MouseButtonEventArgs e)
        {
            System.Diagnostics.Process.Start(Net48Link.Text);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
