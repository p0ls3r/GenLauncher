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
    /// Логика взаимодействия для AddModificationWindow.xaml
    /// </summary>
    public partial class AddModificationWindow : Window
    {
        public event Action<string> AddModification;

        public AddModificationWindow(List<string> modificationsNames)
        {
            InitializeComponent();
            ModificationsList.ItemsSource = modificationsNames;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (ModificationsList.SelectedItem != null)
            {
                AddModification(ModificationsList.SelectedItem.ToString());
                this.Close();
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
