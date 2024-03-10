using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace GenLauncherNet
{
    public class ModificationViewModel : UserControl
    {
        public GameModification ContainerModification { get; private set; }

        public ModificationVersion LatestVersion { get; private set; }
        public ModificationVersion SelectedVersion { get; private set; }

        public IUpdater Downloader { get; set; }
        public GridControls _GridControls { get; private set; }
        public BitmapImage ImageSource { get; }

        public string NameInfo { get; private set; }
        public string LatestVersionInfo { get; private set; }
        public bool ReadyToRun { get; set; } = true;

        public ColorsInfo Colors { get; set; }

        public bool LocalMod { get; set; }

        private int imageIndex = -1;

        public ModificationViewModel(GameModification modification)
        {
            ContainerModification = modification;

            UpdataContainerData();
        }

        public void UpdataContainerData()
        {
            NameInfo = ContainerModification.Name;

            if (ContainerModification.ModificationVersions != null && ContainerModification.ModificationVersions.Count > 0)
            {
                var ContainerVersions = ContainerModification.ModificationVersions.OrderBy(m => m);

                if (ContainerModification.ModificationType == ModificationType.Mod)
                {
                    LocalMod = !ContainerModification.ModificationVersions.Where(v => !string.IsNullOrEmpty(v.S3BucketName) || !string.IsNullOrEmpty(v.SimpleDownloadLink) || !string.IsNullOrEmpty(v.S3FolderName)).Any();
                }

                SelectedVersion = GetSelectedVersion();
                if (SelectedVersion != null)
                  SelectedVersion.IsSelected = true;

                LatestVersion = ContainerVersions.Last();

                if (ContainerModification.ModificationType == ModificationType.Advertising)
                {
                    LatestVersionInfo = LatestVersion.Version;
                }
                else
                    LatestVersionInfo = LocalizedStrings.Instance["LatestVersion"] + LatestVersion.Version;
            }
        }

        public void SetDragAndDropMod()
        {
            _GridControls._DragAndDropRectangle.Visibility = Visibility.Visible;
            SetSelectedStatus();
        }

        public void RemoveDragAndDropMod()
        {
            _GridControls._DragAndDropRectangle.Visibility = Visibility.Hidden;
            SetUnSelectedStatus();
        }

        private void SetBlackWhiteImage()
        {
            if (ContainerModification.ModificationType == ModificationType.Mod)
            {
                var imageFileName = System.IO.Path.Combine(EntryPoint.LauncherFolder, "uam.jpg");
                if (LocalMod)
                {
                    if (File.Exists(System.IO.Path.Combine(EntryPoint.LauncherFolder, EntryPoint.LauncherImageSubFolder, ContainerModification.Name, LatestVersion.Version)))
                        imageFileName = System.IO.Path.Combine(EntryPoint.LauncherFolder, EntryPoint.LauncherImageSubFolder, ContainerModification.Name, LatestVersion.Version);
                }
                else
                {
                    imageFileName = System.IO.Path.Combine(EntryPoint.LauncherFolder, EntryPoint.LauncherImageSubFolder, ContainerModification.Name, LatestVersion.Version);
                }

                if (!File.Exists(imageFileName))
                    imageFileName = System.IO.Path.Combine(EntryPoint.LauncherFolder, "uam.jpg");

                var imageFileNameBW = imageFileName + "BW";

                if (!File.Exists(imageFileNameBW))
                {
                    var image = BlackWhiteImageGenerator.GenerateBlackWhiteBitMapImageFromPath(imageFileName);
                    if (image != null)
                        image.Save(imageFileNameBW);
                }

                SetImage(imageFileNameBW);
            }

            if (ContainerModification.ModificationType == ModificationType.Advertising)
            {
                var folderName = ContainerModification.Name.Trim(System.IO.Path.GetInvalidFileNameChars());

                var dirInfo = new DirectoryInfo(System.IO.Path.Combine(EntryPoint.LauncherFolder, EntryPoint.LauncherImageSubFolder, folderName));

                var filesCount = dirInfo.GetFiles().Length;

                if (imageIndex == -1)
                {
                    var rand = new Random();

                    var k = rand.Next(0, 30);

                    if (k == 0)
                        imageIndex = rand.Next(filesCount / 2, filesCount);
                    else
                        imageIndex = rand.Next(0, filesCount / 2);
                }

                var imageFileName = System.IO.Path.Combine(EntryPoint.LauncherFolder, EntryPoint.LauncherImageSubFolder, folderName, imageIndex.ToString());

                SetImage(imageFileName);
            }
        }

        private void SetColorfullImage()
        {
            if (ContainerModification.ModificationType == ModificationType.Mod)
            {
                var imageFileName = System.IO.Path.Combine(EntryPoint.LauncherFolder, "uam.jpg");
                if (LocalMod)
                {
                    if (File.Exists(System.IO.Path.Combine(EntryPoint.LauncherFolder, EntryPoint.LauncherImageSubFolder, ContainerModification.Name, LatestVersion.Version)))
                        imageFileName = System.IO.Path.Combine(EntryPoint.LauncherFolder, EntryPoint.LauncherImageSubFolder, ContainerModification.Name, LatestVersion.Version);
                }
                else
                {
                    imageFileName = System.IO.Path.Combine(EntryPoint.LauncherFolder, EntryPoint.LauncherImageSubFolder, ContainerModification.Name, LatestVersion.Version);
                }

                if (!File.Exists(imageFileName))
                    imageFileName = System.IO.Path.Combine(EntryPoint.LauncherFolder, "uam.jpg");

                SetImage(imageFileName);
            }
        }

        private void SetImage(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return;

            var stream = File.OpenRead(path);

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
                    if (File.Exists(path))
                        File.Delete(path);
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

                UpdataContainerData();

                if (ContainerModification.ModificationVersions.Where(m => m.Installed).Any())
                {
                    ReadyToRun = true;
                }
            }
            SetUnactiveProgressBar();
        }

        public void BruteCancelDownload()
        {
            if (Downloader != null)
            {
                Downloader.Dispose();
            }
        }

        public void Refresh()
        {
            if (_GridControls != null)
            {
                _GridControls._UpdateButton.Refresh();
                _GridControls._SupportButton.Refresh();

                if (Downloader == null)
                    SetUnactiveProgressBar();
                else
                    SetActiveProgressBar();

                if (ContainerModification.IsSelected)
                    SetSelectedStatus();
                else
                    SetUnSelectedStatus();
            }
        }

        public void SetSelectedStatus()
        {
            _GridControls._Name.Foreground = EntryPoint.Colors.GenLauncherActiveColor;
            _GridControls._VersionTextBlock.Foreground = EntryPoint.Colors.GenLauncherDefaultTextColor;
            _GridControls._Name.FontWeight = FontWeights.Bold;

            if (_GridControls._NetworkInfo != null && !String.IsNullOrEmpty(ContainerModification.NetworkInfo))
                _GridControls._NetworkInfo.Visibility = System.Windows.Visibility.Visible;
            if (_GridControls._ChangeLogButton != null && !String.IsNullOrEmpty(ContainerModification.NewsLink))
                _GridControls._ChangeLogButton.Visibility = System.Windows.Visibility.Visible;
            if (_GridControls._SupportButton != null && !String.IsNullOrEmpty(ContainerModification.SupportLink))
                _GridControls._SupportButton.Visibility = System.Windows.Visibility.Visible;

            SetColorfullImage();


            if (ContainerModification.ModificationType == ModificationType.Advertising)
            {
                _GridControls._ComboBox.Visibility = Visibility.Hidden;
            }
            else
            {
                _GridControls._ComboBox.Visibility = Visibility.Visible;
            }

            if (_GridControls._Image != null && _GridControls._ImageBorder != null)
            {
                if (_GridControls._Image.Source == null)
                    _GridControls._ImageBorder.BorderThickness = new Thickness(0, 0, 0, 0);
                else
                {
                    _GridControls._ImageBorder.BorderThickness = new Thickness(2, 2, 2, 2);
                    _GridControls._ImageBorder.BorderBrush = EntryPoint.Colors.GenLauncherActiveColor;
                }
            }
        }

        public void SetUnSelectedStatus()
        {
            if (_GridControls == null)
                return;

            _GridControls._Name.Foreground = EntryPoint.Colors.GenLauncherInactiveBorder;
            _GridControls._VersionTextBlock.Foreground = EntryPoint.Colors.GenLauncherInactiveBorder;
            _GridControls._Name.FontWeight = FontWeights.Normal;

            _GridControls._ComboBox.Visibility = System.Windows.Visibility.Hidden;

            SetBlackWhiteImage();

            if (ContainerModification.ModificationType != ModificationType.Advertising)
            {
                if (_GridControls._NetworkInfo != null)
                    _GridControls._NetworkInfo.Visibility = System.Windows.Visibility.Hidden;
                if (_GridControls._ChangeLogButton != null)
                    _GridControls._ChangeLogButton.Visibility = System.Windows.Visibility.Hidden;
                if (_GridControls._SupportButton != null)
                    _GridControls._SupportButton.Visibility = System.Windows.Visibility.Hidden;
            }

            if (_GridControls._Image != null && _GridControls._ImageBorder != null)
            {
                if (_GridControls._Image.Source == null)
                    _GridControls._ImageBorder.BorderThickness = new Thickness(0, 0, 0, 0);
                else
                {
                    _GridControls._ImageBorder.BorderThickness = new Thickness(2, 2, 2, 2);
                    _GridControls._ImageBorder.BorderBrush = EntryPoint.Colors.GenLauncherInactiveBorder;
                }
            }

        }

        public void SetUnactiveProgressBar()
        {
            if (_GridControls != null && _GridControls._ProgressBar != null)
            {
                this._GridControls._ProgressBar.Background = EntryPoint.Colors.GenLauncherDarkBackGround;
                this._GridControls._ProgressBar.BorderBrush = EntryPoint.Colors.GenLauncherInactiveBorder;
                this._GridControls._InfoTextBlock.Foreground = EntryPoint.Colors.GenLauncherDefaultTextColor;
            }
        }

        public void SetActiveProgressBar()
        {
            if (_GridControls != null && _GridControls._ProgressBar != null)
            {
                this._GridControls._ProgressBar.Background = new SolidColorBrush(EntryPoint.Colors.GenLauncherButtonSelectionColor);
                this._GridControls._ProgressBar.BorderBrush = EntryPoint.Colors.GenLauncherBorderColor;
                this._GridControls._InfoTextBlock.Foreground = EntryPoint.Colors.GenLauncherDownloadTextColor;
                if (ContainerModification.ModificationType == ModificationType.Mod)
                {
                    this._GridControls._UpdateRectangle.Visibility = Visibility.Hidden;
                }
            }
        }

        public void SetUIElements(GridControls gridControls)
        {
            _GridControls = gridControls;
            _GridControls._UpdateButton.Refresh();
            UpdateUIelements();

            if (LatestVersion != null && (LatestVersion.ModificationType == ModificationType.Mod || LatestVersion.ModificationType == ModificationType.Advertising))
                SetBlackWhiteImage();

            if (ContainerModification.IsSelected)
                SetSelectedStatus();
            else
                SetUnSelectedStatus();

            if (!ReadyToRun && Downloader != null)
                PrepareControlsToDownloadMode();
        }

        public void UpdateUIelements()
        {
            if (Downloader == null || Downloader.GetResult().Crashed)
            {
                Downloader = null;
                this._GridControls._ProgressBar.Value = 0;
                this._GridControls._InfoTextBlock.Text = String.Empty;

                if (ContainerModification.ModificationType != ModificationType.Advertising)
                    this._GridControls._UpdateButton.Content = LocalizedStrings.Instance["Update"];
                else
                {
                    this._GridControls._UpdateButton.Content = "Donation Alerts";
                    if (String.IsNullOrEmpty(ContainerModification.SimpleDownloadLink))
                            this._GridControls._UpdateButton.Visibility = Visibility.Hidden;

                    this._GridControls._ChangeLogButton.Content = "Boosty.to";
                    this._GridControls._NetworkInfo.Content = "My Youtube (Ru)";
                    this._GridControls._InfoTextBlock.Foreground = EntryPoint.Colors.GenLauncherDefaultTextColor;
                }

                UpdataContainerData();

                if (ContainerModification.ModificationType != ModificationType.Advertising)
                {
                    UpdateComboBox();
                    SelectItemInComboBox();
                }
                else
                {
                    _GridControls._ComboBox.Visibility = Visibility.Hidden;
                }
            }

            if (String.IsNullOrEmpty(ContainerModification.NewsLink))
                _GridControls._ChangeLogButton.Visibility = Visibility.Hidden;

            if (String.IsNullOrEmpty(ContainerModification.NetworkInfo))
                _GridControls._NetworkInfo.Visibility = Visibility.Hidden;

            if (String.IsNullOrEmpty(ContainerModification.SupportLink) && _GridControls._SupportButton != null)
                _GridControls._SupportButton.Visibility = Visibility.Hidden;
        }

        public void UpdateComboBox()
        {
            if (LatestVersion != null && LatestVersion.Installed)
            {
                this._GridControls._UpdateButton.IsEnabled = false;
                if (ContainerModification.ModificationType == ModificationType.Mod)
                {
                    this._GridControls._UpdateRectangle.Visibility = Visibility.Hidden;
                }
            }
            else
            {
                this._GridControls._UpdateButton.IsEnabled = true;
                if (ContainerModification.ModificationType == ModificationType.Mod)
                {
                    this._GridControls._UpdateRectangle.Visibility = Visibility.Visible;
                    this._GridControls._UpdateButton.IsBlinking = true;
                }
            }

            var versionListSource = new ObservableCollection<ComboBoxData>();

            foreach (var version in ContainerModification.ModificationVersions.Where(m => m.Installed).OrderBy(m => m))
            {
                versionListSource.Add(new ComboBoxData(this.ContainerModification, version.Version, this));
            }

            this._GridControls._ComboBox.ItemsSource = versionListSource;
        }

        public void SelectItemInComboBox()
        {
            //Case there is no repos version and no VersionsInstalled
            if (ContainerModification.ModificationVersions.Count == 0)
            {
                _GridControls._ComboBox.IsEnabled = false;
                return;
            }

            //Case there is Repos version for mod and no local versions installed
            if (ContainerModification.ModificationVersions.Count == 1 && !LatestVersion.Installed)
            {
                SetUIToInstallMode();
                return;
            }

            //Case there is installed and selected Version in data, select it, otherwise select latest installed version
            _GridControls._ComboBox.IsEnabled = true;
            if (ReadyToRun)
            {
                var versionString = SelectedVersion.Version;
                var ItemIndex = GetIndexOfItemByName(_GridControls._ComboBox, versionString);
                if (ItemIndex != -1)
                    _GridControls._ComboBox.SelectedItem = _GridControls._ComboBox.Items[ItemIndex];
            }
            //Case there was installation, select last version
            else
            {
                if (SelectedVersion != null)
                    SelectedVersion.IsSelected = false;

                SelectedVersion = ContainerModification.ModificationVersions.Where(m => m.Installed).Last();
                SelectedVersion.IsSelected = true;
                var versionString = SelectedVersion.Version;
                var ItemIndex = GetIndexOfItemByName(_GridControls._ComboBox, versionString);
                if (ItemIndex != -1)
                    _GridControls._ComboBox.SelectedItem = _GridControls._ComboBox.Items[ItemIndex];

                ReadyToRun = true;
            }
        }

        private ModificationVersion GetSelectedVersion()
        {
            if (ContainerModification.ModificationVersions.Count > 0)
            {
                var sv = ContainerModification.ModificationVersions.Where(m => m.Installed).Where(m => m.IsSelected).FirstOrDefault();

                if (sv != null)
                    return sv;

                sv = ContainerModification.ModificationVersions.Where(m => m.Installed).OrderBy(m => m).FirstOrDefault();

                if (sv != null)
                    return sv;

                sv = ContainerModification.ModificationVersions.OrderBy(m => m).FirstOrDefault();

                if (sv != null)
                    return sv;
            }

            return null;
        }

        public void SetDownloader(IUpdater downloader)
        {
            Downloader = downloader;
        }

        public void ClearDownloader()
        {
            Downloader = null;
        }

        public void PrepareControlsToDownloadMode()
        {
            this._GridControls._UpdateButton.Content = LocalizedStrings.Instance["Stop"];
            this._GridControls._ComboBox.IsEnabled = false;
            this.ReadyToRun = false;

            SetActiveProgressBar();
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
            this._GridControls._UpdateButton.Content = LocalizedStrings.Instance["Install"];
            this.ReadyToRun = false;

            if (ContainerModification.ModificationType == ModificationType.Mod)
            {
                this._GridControls._UpdateRectangle.Visibility = Visibility.Hidden;
            }
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