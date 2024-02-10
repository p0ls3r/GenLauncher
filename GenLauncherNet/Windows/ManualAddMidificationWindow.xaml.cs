using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
            SetColors();
        }

        private void SetColors()
        {
            this.Resources["GenLauncherBorderColor"] = EntryPoint.Colors.GenLauncherBorderColor;
            this.Resources["GenLauncherActiveColor"] = EntryPoint.Colors.GenLauncherActiveColor;
            this.Resources["GenLauncherDarkFillColor"] = EntryPoint.Colors.GenLauncherDarkFillColor;
            this.Resources["GenLauncherInactiveBorder"] = EntryPoint.Colors.GenLauncherInactiveBorder;
            this.Resources["GenLauncherInactiveBorder2"] = EntryPoint.Colors.GenLauncherInactiveBorder2;
            this.Resources["GenLauncherDefaultTextColor"] = EntryPoint.Colors.GenLauncherDefaultTextColor;
            this.Resources["GenLauncherLightBackGround"] = EntryPoint.Colors.GenLauncherLightBackGround;
            this.Resources["GenLauncherDarkBackGround"] = EntryPoint.Colors.GenLauncherDarkBackGround;
            this.Resources["GenLauncherDefaultTextColor"] = EntryPoint.Colors.GenLauncherDefaultTextColor;

            this.Resources["GenLauncherListBoxSelectionColor2"] = EntryPoint.Colors.GenLauncherListBoxSelectionColor2;
            this.Resources["GenLauncherListBoxSelectionColor1"] = EntryPoint.Colors.GenLauncherListBoxSelectionColor1;
            this.Resources["GenLauncherButtonSelectionColor"] = EntryPoint.Colors.GenLauncherButtonSelectionColor;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrEmpty(ModificationName.Text))
            {
                CreateErrorWindow(LocalizedStrings.Instance["OperationAborted"], LocalizedStrings.Instance["EnterModName"]);
                return;
            }

            if (String.IsNullOrEmpty(Version.Text))
            {
                CreateErrorWindow(LocalizedStrings.Instance["OperationAborted"], LocalizedStrings.Instance["EnterModVersion"]);
                return;
            }
            else
            {
                var digits = new string(Version.Text.Where(n => n >= '0' && n <= '9').ToArray());

                if (digits.Length == 0)
                {
                    CreateErrorWindow(LocalizedStrings.Instance["OperationAborted"], LocalizedStrings.Instance["VersionMustContainNumbers"]);
                    return;
                }
            }

            var validCharactersModificationName = CleanInput(ModificationName.Text);
            var validCharactersVersion = CleanInput(Version.Text);

            if (validCharactersModificationName.Length == 0 || validCharactersVersion.Length == 0)
            {
                CreateErrorWindow(LocalizedStrings.Instance["OperationAborted"], LocalizedStrings.Instance["NameAndVersionValidSymbols"]);
                return;
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

        static string CleanInput(string strIn)
        {
            // Replace invalid characters with empty strings.
            try
            {
                return Regex.Replace(strIn, @"[^\w\.@-]", "",
                                     RegexOptions.None, TimeSpan.FromSeconds(1.5));
            }
            // If we timeout when replacing invalid characters,
            // we should return Empty.
            catch (RegexMatchTimeoutException)
            {
                return String.Empty;
            }
        }

        private void CreateErrorWindow(string mainMessage, string modMessage)
        {
            var infoWindow = new InfoWindow(mainMessage, modMessage) { WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen };
            infoWindow.Continue.Visibility = Visibility.Hidden;
            infoWindow.Cancel.Visibility = Visibility.Hidden;
            infoWindow.ErrorPolygon1.Visibility = Visibility.Visible;
            infoWindow.ErrorPolygon2.Visibility = Visibility.Visible;

            infoWindow.ShowDialog();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
