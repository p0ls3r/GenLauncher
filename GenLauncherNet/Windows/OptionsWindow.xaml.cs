using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Linq;

namespace GenLauncherNet
{
    public partial class OptionsWindow : Window
    {
        private Dictionary<string, string> gameOptions = new Dictionary<string, string>();
        private string optionsFilePath;
        private bool gameOptionsChanged;

        public OptionsWindow()
        {
            if (EntryPoint.SessionInfo.GameMode == Game.ZeroHour)
                optionsFilePath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\Command and Conquer Generals Zero Hour Data" + "/Options.ini";
            else
                optionsFilePath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\Command and Conquer Generals Data" + "/Options.ini";

            CheckGameOptionsFile();
            ReadOptions();
            ValidateOptions();
            InitializeComponent();
            InitUIStatus();
            SetColors();
            this.MouseDown += Window_MouseDown;
            Resolution.SelectionChanged += Resolution_SelectionChanged;
            gameOptionsChanged = false;
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

        private void InitUIStatus()
        {
            if (EntryPoint.SessionInfo.GameMode == Game.Generals)
            {
                DataHandler.SetModdedExeStatus(false);
                modded.IsEnabled = false;
                generals.IsEnabled = false;

                customCamera.IsEnabled = false;
                defaultCamera.IsEnabled = false;
                CameraHeightSlider.IsEnabled = false;
                DataHandler.SetCameraHeight(0);
            }

            FillResolutionComboBox();
            SetParticleSliderValue();
            SetTextureReductionSliderValue();
            SetOptionsToggles();

            if (DataHandler.IsModdedExe())
            {
                modded.IsChecked = true;
            }
            else
            {
                generals.IsChecked = true;
            }

            if (DataHandler.GetHideLauncher())
            {
                HideLauncher.IsChecked = true;
            }
            else
            {
                HideLauncher.IsChecked = false;
            }

            if (DataHandler.GetCheckModFiles())
            {
                CheckModFiles.IsChecked = true;
            }
            else
            {
                CheckModFiles.IsChecked = false;
                AskToCheck.Visibility = Visibility.Hidden;
            }

            if (DataHandler.GetAskBeforeCheck())
            {
                AskToCheck.IsChecked = true;
            }
            else
            {
                AskToCheck.IsChecked = false;
            }

            if (DataHandler.GetCameraHeight() == 0)
            {
                defaultCamera.IsChecked = true;
                CameraHeightSlider.Visibility = Visibility.Hidden;
                CameraHeightLabel.Visibility = Visibility.Hidden;
            }
            else
            {
                customCamera.IsChecked = true;
                CameraHeightSlider.Value = DataHandler.GetCameraHeight();
            }

            if (DataHandler.GentoolAutoUpdate())
            {
                InstallGentool.IsChecked = true;
            }
            else
            {
                DisableGentool.IsChecked = true;
            }

            GameParams.Text = DataHandler.GetGameParams();

            if (DataHandler.GetAutoDeleteOldVersionsOption())
                DeleteOldVersion.IsChecked = true;
        }

        private void SetOptionsToggles()
        {
            if (gameOptions["UseShadowVolumes"] == " yes")
                VolumeShadows.IsChecked = true;

            if (gameOptions["BuildingOcclusion"] == " yes")
                BehindBuilding.IsChecked = true;

            if (gameOptions["UseShadowDecals"] == " yes")
                Shadows.IsChecked = true;

            if (gameOptions["ShowTrees"] == " yes")
                ShowProps.IsChecked = true;

            if (gameOptions["UseCloudMap"] == " yes")
                CloudShadows.IsChecked = true;

            if (gameOptions["ExtraAnimations"] == " yes")
                ExtraAnimation.IsChecked = true;

            if (gameOptions["UseLightMap"] == " yes")
                ExtraGroundLighting.IsChecked = true;

            if (gameOptions["DynamicLOD"] == " no")
                DisableDynamicLevelOfDetail.IsChecked = true;

            if (gameOptions["ShowSoftWaterEdge"] == " yes")
                SmoothWaterBorders.IsChecked = true;

            if (gameOptions["HeatEffects"] == " yes")
                HeatEffects.IsChecked = true;

            if (gameOptions["UseAlternateMouse"] == " yes")
                AlternateMouseSetup.IsChecked = true;
        }

        private void SetParticleSliderValue()
        {
            var value = gameOptions["MaxParticleCount"];

            if (Int32.TryParse(value, out int result))
                MaxParticleCountSlider.Value = result;
            else
            {
                gameOptionsChanged = true;
                MaxParticleCountSlider.Value = 100;
                MaxParticleCountLabel.Content = MaxParticleCountSlider.Value;
            }
        }

        private void SetTextureReductionSliderValue()
        {
            var value = gameOptions["TextureReduction"];

            if (Int32.TryParse(value, out int result))
            {
                TextureReductionSlider.Value = InvertTextureReductionValue(result);
                TextureReductionLabel.Content = InvertTextureReductionValue(result);
            }
            else
            {
                gameOptionsChanged = true;
                TextureReductionSlider.Value = 2;
                TextureReductionLabel.Content = TextureReductionSlider.Value;
            }
        }

        private int InvertTextureReductionValue(int value)
        {
            return Math.Abs(value - 2);
        }

        private void FillResolutionComboBox()
        {
            Resolution.Items.Add("1024×768");
            Resolution.Items.Add("1152×864");
            Resolution.Items.Add("1176×664");
            Resolution.Items.Add("1280×720");
            Resolution.Items.Add("1280×768");

            Resolution.Items.Add("1280×800");
            Resolution.Items.Add("1280×960");
            Resolution.Items.Add("1280×1024");
            Resolution.Items.Add("1360×768");
            Resolution.Items.Add("1366×768");
            Resolution.Items.Add("1440×900");
            Resolution.Items.Add("1600×900");
            Resolution.Items.Add("1600×1024");
            Resolution.Items.Add("1600×1200");

            Resolution.Items.Add("1680×1050");
            Resolution.Items.Add("1920×1080");
            Resolution.Items.Add("1920×1200");
            Resolution.Items.Add("1920×1440");

            Resolution.Items.Add("2560×1440");
            Resolution.Items.Add("3620×2036");

            var resolution = gameOptions["Resolution"].Substring(1).Replace(" ", "×");

            if (!Resolution.Items.Contains(resolution))
                Resolution.Items.Add(resolution);

            Resolution.SelectedItem = resolution;
        }

        private void CheckGameOptionsFile()
        {
            var folderPath = string.Empty;

            if (EntryPoint.SessionInfo.GameMode == Game.ZeroHour)
               folderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\Command and Conquer Generals Zero Hour Data";
            else
                folderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\Command and Conquer Generals Data";

            if (!OptionsFileExists(folderPath))
                ExtractOptions(folderPath);
        }

        private void ExtractOptions(string folderPath)
        {
            using (var resource = Assembly.GetExecutingAssembly().GetManifestResourceStream("GenLauncherNet.Options.options.ini"))
            {
                using (var file = new FileStream(folderPath + "/Options.ini", FileMode.Create, FileAccess.Write))
                {
                    resource?.CopyTo(file);
                }
            }
        }

        private void ValidateOptions()
        {
            if (!gameOptions.ContainsKey("Resolution"))
            {
                gameOptions.Add("Resolution", "1024×768");
                gameOptionsChanged = true;
            }

            if (!gameOptions.ContainsKey("MaxParticleCount"))
            {
                gameOptions.Add("MaxParticleCount", "2500");
                gameOptionsChanged = true;
            }

            if (!gameOptions.ContainsKey("TextureReduction"))
            {
                gameOptions.Add("TextureReduction", "1");
                gameOptionsChanged = true;
            }

            gameOptions.TryAdd("UseShadowVolumes", " no");
            gameOptions.TryAdd("BuildingOcclusion", " no");
            gameOptions.TryAdd("UseShadowDecals", " no");
            gameOptions.TryAdd("ShowTrees", " no");
            gameOptions.TryAdd("UseCloudMap", " no");
            gameOptions.TryAdd("ExtraAnimations", " no");
            gameOptions.TryAdd("UseLightMap", " no");
            gameOptions.TryAdd("DynamicLOD", " yes");
            gameOptions.TryAdd("ShowSoftWaterEdge", " no");
            gameOptions.TryAdd("HeatEffects", " no");
            gameOptions.TryAdd("UseAlternateMouse", " no");
        }

        private void ReadOptions()
        {
            foreach (string line in File.ReadLines(optionsFilePath))
            {
                if (!line.Contains('='))
                    continue;

                var key = line.Split('=')[0].Replace(" ", String.Empty);
                var value = line.Split('=')[1];

                if (String.IsNullOrEmpty(value))
                    continue;

                gameOptions.Add(key, value);
            }
        }

        private void SaveOptions()
        {
            File.WriteAllLines(optionsFilePath, gameOptions.Select(t => t.Key + " =" + (t.Value[0] == ' ' ? t.Value : " " + t.Value)));
        }

        private bool OptionsFileExists(string folderPath)
        {

            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            var filePath = folderPath + "\\Options.ini";

            return File.Exists(filePath);
        }

        private void modded_Click(object sender, RoutedEventArgs e)
        {
            modded.IsChecked = true;
            generals.IsChecked = false;
            DataHandler.SetModdedExeStatus(true);
        }

        private void generals_Click(object sender, RoutedEventArgs e)
        {
            modded.IsChecked = false;
            generals.IsChecked = true;
            DataHandler.SetModdedExeStatus(false);
        }

        private void Apply_Click(object sender, RoutedEventArgs e)
        {
            if (gameOptionsChanged)
                SaveOptions();

            DataHandler.SetGameParams(GameParams.Text);
            this.Close();
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (CameraHeightLabel != null)
            {
                CameraHeightLabel.Content = CameraHeightSlider.Value;
                DataHandler.SetCameraHeight((int)CameraHeightSlider.Value);
            }
        }

        private void RadioButton_Click(object sender, RoutedEventArgs e)
        {
            CameraHeightSlider.Visibility = Visibility.Hidden;
            CameraHeightLabel.Visibility = Visibility.Hidden;
            DataHandler.SetCameraHeight(0);
        }

        private void RadioButton_Click_1(object sender, RoutedEventArgs e)
        {
            CameraHeightSlider.Visibility = Visibility.Visible;
            CameraHeightLabel.Visibility = Visibility.Visible;
            CameraHeightSlider.Value = CameraHeightSlider.Minimum;
            CameraHeightLabel.Content = CameraHeightSlider.Minimum;
            DataHandler.SetCameraHeight((int)CameraHeightSlider.Minimum);
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (Mouse.LeftButton == MouseButtonState.Pressed)
                this.DragMove();
        }

        private void InstallGentool_Click(object sender, RoutedEventArgs e)
        {
            DataHandler.SetGentoolAutoUpdateStatus(true);
        }

        private void DisableGentool_Click(object sender, RoutedEventArgs e)
        {
            DataHandler.SetGentoolAutoUpdateStatus(false);
        }

        private void DeleteOldVersion_Click(object sender, RoutedEventArgs e)
        {
            var check = DataHandler.GetAutoDeleteOldVersionsOption();

            DataHandler.SetAutoDeleteOldVersionsOption(!check);
            DeleteOldVersion.IsChecked = !check;
        }

        private void CheckModFiles_Click(object sender, RoutedEventArgs e)
        {
            var check = DataHandler.GetCheckModFiles();

            DataHandler.SetCheckModFiles(!check);
            CheckModFiles.IsChecked = !check;

            if (DataHandler.GetCheckModFiles())
                AskToCheck.Visibility = Visibility.Visible;
            else
                AskToCheck.Visibility = Visibility.Hidden;
        }

        private void GenLauncherDiscord_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://discord.gg/fFGpudz5hV");
        }

        private void Authors_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://github.com/p0ls3r/GenLauncherModsData/blob/master/Authors.txt");
        }

        private void Sponsors_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://github.com/p0ls3r/GenLauncherModsData/blob/master/Sponsors.txt");
        }

        private void DonationAlerts_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://www.donationalerts.com/r/pal_ser");
        }

        private void Boosty_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://boosty.to/genlauncher");
        }

        private void Resolution_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            var selectedResolution = (string)Resolution.SelectedItem;

            gameOptionsChanged = true;

            gameOptions["Resolution"] = " " + selectedResolution.Replace("×", " ");
        }

        private void MaxParticleCountSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (MaxParticleCountLabel != null)
            {
                MaxParticleCountLabel.Content = MaxParticleCountSlider.Value;

                gameOptionsChanged = true;

                gameOptions["MaxParticleCount"] = MaxParticleCountSlider.Value.ToString();
            }
        }

        private void TextureReductionSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (TextureReductionLabel != null)
            {
                TextureReductionLabel.Content = TextureReductionSlider.Value;

                gameOptionsChanged = true;

                gameOptions["TextureReduction"] = InvertTextureReductionValue((int)TextureReductionSlider.Value).ToString();
            }
        }

        private void VolumeShadows_Click(object sender, RoutedEventArgs e)
        {
            var key = "UseShadowVolumes";
            ChangeGameOptionByKey(key, sender);
        }

        private void BehindBuilding_Click(object sender, RoutedEventArgs e)
        {
            var key = "BuildingOcclusion";
            ChangeGameOptionByKey(key, sender);
        }

        private void Shadows_Click(object sender, RoutedEventArgs e)
        {
            var key = "UseShadowDecals";
            ChangeGameOptionByKey(key, sender);
        }

        private void ShowProps_Click(object sender, RoutedEventArgs e)
        {
            var key = "ShowTrees";
            ChangeGameOptionByKey(key, sender);
        }

        private void CloudShadows_Click(object sender, RoutedEventArgs e)
        {
            var key = "UseCloudMap";
            ChangeGameOptionByKey(key, sender);
        }

        private void ExtraAnimation_Click(object sender, RoutedEventArgs e)
        {
            var key = "ExtraAnimations";
            ChangeGameOptionByKey(key, sender);
        }

        private void ExtraGroundLighting_Click(object sender, RoutedEventArgs e)
        {
            var key = "UseLightMap";
            ChangeGameOptionByKey(key, sender);
        }

        private void DisableDynamicLevelOfDetail_Click(object sender, RoutedEventArgs e)
        {
            var key = "DynamicLOD";
            if (gameOptions[key] == " no")
            {
                DisableDynamicLevelOfDetail.IsChecked = false;
                gameOptions[key] = " yes";
            }
            else
            {
                DisableDynamicLevelOfDetail.IsChecked = true;
                gameOptions[key] = " no";
            }

            gameOptionsChanged = true;
        }

        private void SmoothWaterBorders_Click(object sender, RoutedEventArgs e)
        {
            var key = "ShowSoftWaterEdge";
            ChangeGameOptionByKey(key, sender);
        }

        private void HeatEffects_Click(object sender, RoutedEventArgs e)
        {
            var key = "HeatEffects";
            ChangeGameOptionByKey(key, sender);
        }

        private void AlternateMouseSetup_Click(object sender, RoutedEventArgs e)
        {
            var key = "UseAlternateMouse";
            ChangeGameOptionByKey(key, sender);
        }

        private void ChangeGameOptionByKey(string gameOptionName, object sender)
        {
            var radioButton = (System.Windows.Controls.RadioButton)sender;

            if (gameOptions[gameOptionName] == " yes")
            {
                radioButton.IsChecked = false;
                gameOptions[gameOptionName] = " no";
            }
            else
            {
                radioButton.IsChecked = true;
                gameOptions[gameOptionName] = " yes";
            }

            gameOptionsChanged = true;
        }

        private void HideLauncherSetup_Click(object sender, RoutedEventArgs e)
        {
            var check = DataHandler.GetHideLauncher();

            DataHandler.SetHideLauncher(!check);
            HideLauncher.IsChecked = !check;
        }

        private void AskToCheck_Click(object sender, RoutedEventArgs e)
        {
            var check = DataHandler.GetAskBeforeCheck();

            DataHandler.SetAskBeforeCheck(!check);
            AskToCheck.IsChecked = !check;
        }
    }

    public static class DictionaryExtension
    {
        public static bool TryAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue value)
        {
            if (dictionary == null)
            {
                throw new ArgumentNullException(nameof(dictionary));
            }

            if (!dictionary.ContainsKey(key))
            {
                dictionary.Add(key, value);
                return true;
            }

            return false;
        }
    }
}
