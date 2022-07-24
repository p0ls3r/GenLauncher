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
    /// Interaction logic for ModificationNames.xaml
    /// </summary>
    public partial class ManualAddMidificationWindow : Window
    {
        private List<string> FilesList = new List<string>();
        private string AddonPath;
        public event Action<List<string>, string, string> CreateModCallback;
        public event Action<List<string>, string ,string, string> CreateAddonCallback;

        public ManualAddMidificationWindow(List<string> files, string addonPath = null)
        {
            FilesList = files;
            AddonPath = addonPath;
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrEmpty(ModificationName.Text))
            {
                CreateErrorWindow("Operation aborted", "Enter modification name");
                return;
            }

            if (String.IsNullOrEmpty(Version.Text))
            {
                CreateErrorWindow("Operation aborted", "Enter modification version");
                return;
            }
            else
            {
                var digits = new string(Version.Text.Where(n => n >= '0' && n <= '9').ToArray());

                if (digits.Length == 0)
                {
                    CreateErrorWindow("Operation aborted", "Version must contain numbers");
                    return;
                }
            }

            if (CreateModCallback != null)
            {
                this.Close();
                CreateModCallback(FilesList, ModificationName.Text, Version.Text);
            }

            if (CreateAddonCallback != null)
            {
                this.Close();
                CreateAddonCallback(FilesList, AddonPath, ModificationName.Text, Version.Text);
            }
        }

        private void CreateErrorWindow(string mainMessage, string modMessage)
        {
            var infoWindow = new InfoWindow(mainMessage, modMessage) { WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen };
            infoWindow.Continue.Visibility = Visibility.Hidden;
            infoWindow.Cancel.Visibility = Visibility.Hidden;
            infoWindow.ErrorBG.Visibility = Visibility.Visible;

            infoWindow.ShowDialog();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
