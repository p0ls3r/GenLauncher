using System;
using System.Windows;

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
            SetColors();

            InfoTextBox.Text = String.Format(LocalizedStrings.Instance["NewUpdateForGenLauncher"], VersionName);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
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
    }
}
