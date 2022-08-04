using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace GenLauncherNet
{
    public class ModBoxData
    {
        public ModificationVersion LatestVersion { get; private set; }
        public ModificationVersion ActivatedVersion { get; private set; }
        public List<ModificationVersion> ModificationVersions { get; private set; }

        public ModificationReposVersion ModBoxModification { get; private set; }
        public bool Selected { get; private set; }
        public bool Favorite { get; private set; }

        public bool ReadyToRun { get; set; } = true;
        public ModificationDownloader Downloader { get; set; }
        public string NameInfo { get; private set; }
        public GridControls _GridControls { get; private set; }

        public string LatestVersionInfo { get; private set; }
        public BitmapImage ImageSource { get; }

        public ModBoxData(ModificationReposVersion modification)
        {
            ModBoxModification = modification;

            UpdataModboxData();
        }

        public void UpdataModboxData()
        {
            switch (ModBoxModification.ModificationType)
            {
                case ModificationType.Mod:
                    SetModboxDataFromModificationVersions(DataHandler.GetModVersions(ModBoxModification));
                    break;
                case ModificationType.Addon:
                    SetModboxDataFromModificationVersions(DataHandler.GetAddonVersions(ModBoxModification));
                    break;
                case ModificationType.Patch:
                    SetModboxDataFromModificationVersions(DataHandler.GetPatchVersions(ModBoxModification));
                    break;
            }

            CheckFavoriteStatus();
        }

        private void CheckFavoriteStatus()
        {
            foreach (var version in ModificationVersions)
                if (version.Favorite)
                {
                    Favorite = true;
                    break;
                }
        }

        private void CheckFavoriteStatusUI()
        {
            if (Favorite)
                SetFavoriteStatus();
        }

        private void SetModboxDataFromModificationVersions(List<ModificationVersion> modificationVersions)
        {
            NameInfo = ModBoxModification.Name;
            if (modificationVersions != null && modificationVersions.Count > 0)
            {
                modificationVersions.Sort();
                ModificationVersions = modificationVersions;
                ActivatedVersion = ModificationVersions.Where(t => t.IsSelected).FirstOrDefault();

                if (ActivatedVersion == null)
                    ActivatedVersion = ModificationVersions.OrderBy(m => m).LastOrDefault();

                ActivatedVersion.IsSelected = true;

                LatestVersion = modificationVersions.Last();
                LatestVersionInfo = "Latest version: " + LatestVersion.Version;
            }

            ModificationVersions = modificationVersions;
        }

        private void SetImage()
        {
            var imageFileName = Path.Combine(EntryPoint.LauncherFolder, EntryPoint.LauncherImageSubFolder, ModBoxModification.Name, ModBoxModification.Version);
            if (!File.Exists(imageFileName))
                return;

            var stream = File.OpenRead(imageFileName);

            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.StreamSource = stream;
                bitmap.EndInit();
                _GridControls._Image.Source = bitmap;
                stream.Close();
            }
            catch
            {
                try
                {
                    stream.Close();
                    if (File.Exists(imageFileName))
                        File.Delete(imageFileName);
                }
                catch
                {
                    //TODO logger
                }
            }
        }

        public void CancelDownload()
        {
            if (Downloader != null)
            {
                _GridControls._UpdateButton.IsEnabled = false;
                Downloader.CancelDownload();
                ClearDownloader();

                UpdataModboxData();

                if (ModificationVersions.Where(m => m.Installed).Any())
                {
                    ReadyToRun = true;
                }
            }
            SetUnactiveProgressBar();
        }

        public void SetSelectedStatus()
        {
            Selected = true;
            _GridControls._Name.Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString("#baff0c");
            _GridControls._VersionTextBlock.Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString("White");
            _GridControls._Name.FontWeight = FontWeights.Bold;
            _GridControls._ComboBox.Visibility = System.Windows.Visibility.Visible;
            
            if (_GridControls._Image != null && _GridControls._ImageBorder != null)
            {
                if (_GridControls._Image.Source == null)
                    _GridControls._ImageBorder.BorderThickness = new Thickness(0, 0, 0, 0);
                else
                {
                    _GridControls._ImageBorder.BorderThickness = new Thickness(2, 2, 2, 2);
                    _GridControls._ImageBorder.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFromString("#baff0c");
                }

            }
        }

        public void SetFavoriteStatus()
        {
            if (_GridControls._MyFavorite == null)
                return;

            Favorite = true;

            foreach (var version in ModificationVersions)
                version.Favorite = true;

            _GridControls._MyFavorite.Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString("White");
            _GridControls._MyFavorite.FontWeight = FontWeight.FromOpenTypeWeight(700);
            _GridControls._MyFavorite.IsChecked = true;
            _GridControls._FavoriteRectangle.Visibility = Visibility.Visible;
        }

        public void SetUnFavoriteStatus()
        {
            _GridControls._MyFavorite.Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString("DarkGray");
            _GridControls._MyFavorite.FontWeight = FontWeight.FromOpenTypeWeight(400);
            _GridControls._MyFavorite.IsChecked = false;

            Favorite = false;

            foreach (var version in ModificationVersions)
                version.Favorite = false;

            _GridControls._FavoriteRectangle.Visibility = Visibility.Hidden;
        }

        public void SetUnSelectedStatus()
        {
            Selected = false;
            _GridControls._Name.Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString("DarkGray");
            _GridControls._VersionTextBlock.Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString("DarkGray");
            _GridControls._Name.FontWeight = FontWeights.Normal;
            _GridControls._ComboBox.Visibility = System.Windows.Visibility.Hidden;


            if (_GridControls._Image != null && _GridControls._ImageBorder != null)
            {
                if (_GridControls._Image.Source == null)
                    _GridControls._ImageBorder.BorderThickness = new Thickness(0, 0, 0, 0);
                else
                {
                    _GridControls._ImageBorder.BorderThickness = new Thickness(2, 2, 2, 2);
                    _GridControls._ImageBorder.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFromString("DarkGray");
                }
            }

        }

        public void SetUnactiveProgressBar()
        {
            if (_GridControls != null && _GridControls._ProgressBar != null)
            {
                this._GridControls._ProgressBar.Background = (SolidColorBrush)new BrushConverter().ConvertFromString("#090502");
                this._GridControls._ProgressBar.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFromString("DarkGray");
                this._GridControls._InfoTextBlock.Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString("White");
            }
        }

        public void SetUIElements(GridControls gridControls)
        {
            _GridControls = gridControls;
            UpdateUIelements();

            if (LatestVersion != null && LatestVersion.ModificationType == ModificationType.Mod)
                SetImage();

            SetUnSelectedStatus();
            CheckFavoriteStatusUI();
        }

        public void UpdateUIelements()
        {
            if (Downloader == null || Downloader.Result.Crashed)
            {
                Downloader = null;
                this._GridControls._ProgressBar.Value = 0;
                this._GridControls._InfoTextBlock.Text = String.Empty;
                this._GridControls._UpdateButton.Content = "UPDATE!";                

                UpdataModboxData();
                UpdateComboBox();
                SelectItemInComboBox();
            }

            if (String.IsNullOrEmpty(ModBoxModification.NewsLink))
                _GridControls._ChangeLogButton.Visibility = Visibility.Hidden;

            if (String.IsNullOrEmpty(ModBoxModification.NetworkInfo))
                _GridControls._NetworkInfo.Visibility = Visibility.Hidden;
        }

        public void UpdateComboBox()
        {
            if (LatestVersion != null && LatestVersion.Installed)
            {
                this._GridControls._UpdateButton.IsEnabled = false;
            }
            else
            {
                this._GridControls._UpdateButton.IsEnabled = true;
                this._GridControls._UpdateButton.IsBlinking = true;
            }

            var versionListSource = new ObservableCollection<ComboBoxData>();

            foreach (var version in ModificationVersions.Where(m => m.Installed))
            {
                versionListSource.Add(new ComboBoxData(this.ModBoxModification, version.Version, this));
            }

            this._GridControls._ComboBox.ItemsSource = versionListSource;
        }

        public void SelectItemInComboBox()
        {
            //Case no VersionsInstalled
            if (ModificationVersions.Count == 0)
            {
                if (!DataHandler.TempAddedMods.Contains(ModBoxModification.Name))
                    DataHandler.RemoveModificationFromAdded(ModBoxModification);

                _GridControls._ComboBox.IsEnabled = false;
                return;
            }

            //Case no VersionsInstalled
            if (ModificationVersions.Count == 1 && !LatestVersion.Installed)
            {
                if (!DataHandler.TempAddedMods.Contains(ModBoxModification.Name))
                    DataHandler.RemoveModificationFromAdded(ModBoxModification);

                SetUIToInstallMode();
                return;
            }

            //Case there is installed Version in data, select it
            _GridControls._ComboBox.IsEnabled = true;
            if (ReadyToRun)
            {
                if (ActivatedVersion == null || !LatestVersion.Installed)
                {
                    ActivatedVersion = ModificationVersions.Where(m => m.Installed).Last();
                }
                var versionString = ActivatedVersion.Version;
                var ItemIndex = GetIndexOfItemByName(_GridControls._ComboBox, versionString);
                if (ItemIndex != -1)
                    _GridControls._ComboBox.SelectedItem = _GridControls._ComboBox.Items[ItemIndex];
            }
            //Case there was installation, select last version
            else
            {
                if (ActivatedVersion != null)
                    ActivatedVersion.IsSelected = false;

                ActivatedVersion = ModificationVersions.Where(m => m.Installed).Last();
                var versionString = ActivatedVersion.Version;
                var ItemIndex = GetIndexOfItemByName(_GridControls._ComboBox, versionString);
                if (ItemIndex != -1)
                    _GridControls._ComboBox.SelectedItem = _GridControls._ComboBox.Items[ItemIndex];

                ReadyToRun = true;
            }
        }

        public void SetDownloader(ModificationDownloader downloader)
        {
            Downloader = downloader;
        }

        public void ClearDownloader()
        {
            Downloader = null;
        }

        public void PrepareControlsToDownloadMode()
        {
            this._GridControls._UpdateButton.Content = "STOP";
            this._GridControls._ComboBox.IsEnabled = false;
            this.ReadyToRun = false;

            this._GridControls._ProgressBar.Background = (SolidColorBrush)new BrushConverter().ConvertFromString("#2534ff");
            this._GridControls._ProgressBar.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFromString("#00e3ff");
            this._GridControls._InfoTextBlock.Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString("Black");
        }

        public void SetUIMessages(string message)
        {
            this._GridControls._InfoTextBlock.Text = message;
        }

        public void SetUIMessages(string message, int percentage)
        {
            this._GridControls._InfoTextBlock.Text = message;
            this._GridControls._ProgressBar.Value = percentage;
        }

        public void SetUIToInstallMode()
        {
            this._GridControls._ComboBox.IsEnabled = false;
            this._GridControls._UpdateButton.Content = "INSTALL!";
            this.ReadyToRun = false;
        }

        private static int GetIndexOfItemByName(System.Windows.Controls.ComboBox box, string itemName)
        {
            if (String.IsNullOrEmpty(itemName))
                return -1;

            var p = box.Items;
            for (var i = 0; i < box.Items.Count; i++)
            {
                if (((ComboBoxData)box.Items[i]).VersionName == itemName)
                    return i;
            }

            return -1;
        }
    }
}
