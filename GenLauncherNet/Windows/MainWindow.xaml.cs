using GenLauncherNet;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;


namespace GenLauncherNet
{
    public partial class MainWindow : Window
    {
        [DllImport("kernel32.dll")]
        private static extern bool AllocConsole();

        private ObservableCollection<ModificationContainer> ModsListSource = new ObservableCollection<ModificationContainer>();
        private ObservableCollection<ModificationContainer> PatchesListSource = new ObservableCollection<ModificationContainer>();
        private ObservableCollection<ModificationContainer> AddonsListSource = new ObservableCollection<ModificationContainer>();

        private CancellationTokenSource tokenSource;
        private bool updating = false;
        private bool _ignoreSelectionFlagMods = false;
        private bool _ignoreSelectionFlagPatches = false;
        private volatile bool _isGameRunning = false;
        private volatile bool _isWBRunning = false;

        private volatile int downloadingCount = 0;
        private Point _dragStartPoint;

        private bool mouseOverVersionList;

        public MainWindow()
        {
            InitializeComponent();

            this.MouseDown += Window_MouseDown;
            this.Closing += MainWindow_Closing;

            ModsList.PreviewMouseMove += ListBox_PreviewMouseMove;

            HideAllLists();
            ModsList.Visibility = Visibility.Visible;
            ManualAddMod.Visibility = Visibility.Visible;
            AddModButton.Visibility = Visibility.Visible;

            if (!EntryPoint.SessionInfo.Connected)
                AddModButton.Visibility = Visibility.Hidden;

            UpdateWindowedStatus();
            UpdateQuickStartStatus();
            UpdateModsList();
            UpdateTabs();

            SetSelfUpdatingInfo(EntryPoint.SessionInfo.Connected);
        }

        private void Exit()
        {
            this.Close();
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            foreach (var patchData in PatchesList.Items)
            {
                var data = (ModificationContainer)patchData;
                data.CancelDownload();
            }

            foreach (var addonData in AddonsList.Items)
            {
                var data = (ModificationContainer)addonData;
                data.CancelDownload();
            }

            foreach (var modData in ModsList.Items)
            {
                var data = (ModificationContainer)modData;
                data.CancelDownload();
            }
        }

        #region SelfUpdater
        private bool IsCurrentVersionOutDated()
        {
            var currentVersionString = new string(EntryPoint.Version.ToCharArray().Where(n => n >= '0' && n <= '9').ToArray());
            var latestVersionString = new string(DataHandler.Version.ToCharArray().Where(n => n >= '0' && n <= '9').ToArray());

            while (currentVersionString.Length > latestVersionString.Length)
                latestVersionString += '0';

            while (currentVersionString.Length < latestVersionString.Length)
                currentVersionString += '0';

            var currentVersion = int.Parse(currentVersionString);
            var latestVersion = int.Parse(latestVersionString);

            return (latestVersion > currentVersion);
        }

        private void LauncherUpdateProgressChanged(long? totalSize, long totalBytesRead, int progressPercentage)
        {
            if (progressPercentage == 100)
                UpdateProgress.Text = "Unpacking...";
            else
            {
                UpdateProgressBar.Value = progressPercentage;
                UpdateProgress.Text = String.Format("{0}MB / {1}MB", (totalBytesRead / 1048576).ToString(), (totalSize / 1048576).ToString());
            }
        }

        private void SetSelfUpdatingInfo(bool connected)
        {
            LauncherVersion.Text = "Version: " + EntryPoint.Version;
            LatestLauncherVersion.Text = "No connection to GitHub";

            if (connected)
            {
                LatestLauncherVersion.Text = "Latest Version: " + DataHandler.Version;
                if (IsCurrentVersionOutDated())
                {
                    LauncherUpdate.IsEnabled = true;
                    LauncherUpdate.IsBlinking = true;

                    var infoWindow = new UpdateAvailable(DataHandler.Version) { WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen };

                    infoWindow.ShowDialog();
                }
            }
        }

        private async void UpdateLauncher()
        {
            tokenSource = new CancellationTokenSource();

            try
            {
                using (var client = new ContentDownloader(DataHandler.DownloadLink, SelfUpdate, "GenLauncherTemp", tokenSource.Token))
                {
                    client.ProgressChanged += LauncherUpdateProgressChanged;
                    client.Done += Exit;
                    SetProgressBarInInstallMode();
                    await client.StartDownload();
                }
            }
            catch (TaskCanceledException)
            {
                UpdateProgressBar.Value = 0;
                SetProgressBarInPassivelMode();
            }
            catch (Exception e)
            {
                UpdateProgressBar.Value = 0;
                UpdateProgress.Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString("White");
                UpdateProgress.Text = "Error" + e.Message;
                SetProgressBarInPassivelMode();
            }
        }

        private void SetProgressBarInInstallMode()
        {
            UpdateProgress.Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString("Black");
            UpdateProgressBar.Background = (SolidColorBrush)new BrushConverter().ConvertFromString("#2534ff");
            UpdateProgressBar.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFromString("#00e3ff");
        }

        private void SetProgressBarInPassivelMode()
        {
            UpdateProgressBar.Background = (SolidColorBrush)new BrushConverter().ConvertFromString("#090502");
            UpdateProgressBar.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFromString("DarkGray");
            UpdateProgressBar.Value = 0;
            UpdateProgress.Text = String.Empty;
        }

        #endregion

        #region ListsContentFillers

        private void UpdatePatchesList()
        {
            PatchesList.ItemsSource = null;
            PatchesListSource = new ObservableCollection<ModificationContainer>();
            PatchesList.Items.Clear();

            var patches = DataHandler.GetPatchesForSelectedMod();

            if (patches.Count > 0)
            {
                PatchesListSource = new ObservableCollection<ModificationContainer>();

                foreach (var patch in patches)
                {
                    PatchesListSource.Add(new ModificationContainer(patch));
                }
            }

            PatchesList.ItemsSource = PatchesListSource;
        }

        private void UpdateAddonsList()
        {
            AddonsList.ItemsSource = null;
            AddonsListSource = new ObservableCollection<ModificationContainer>();
            AddonsList.Items.Clear();

            var addons = DataHandler.GetAddonsForSelectedMod();

            if (addons.Count > 0)
            {
                AddonsListSource = new ObservableCollection<ModificationContainer>();

                foreach (var addon in addons)
                {
                    AddonsListSource.Add(new ModificationContainer(addon));
                }
            }

            AddonsList.ItemsSource = AddonsListSource;
        }

        private void UpdateModsList()
        {
            ModsList.ItemsSource = null;
            ModsListSource = new ObservableCollection<ModificationContainer>();
            ModsList.Items.Clear();

            var mods = DataHandler.GetMods().OrderBy(m => m.NumberInList).ToList();

            if (mods.Count > 0)
            {
                ModsListSource = new ObservableCollection<ModificationContainer>();

                foreach (var mod in mods)
                {
                    ModsListSource.Add(new ModificationContainer(mod));
                }
            }

            ModsList.ItemsSource = ModsListSource;
        }

        public async void AddModToList(string modName)
        {
            DisableUI();

            var modVersion = await DataHandler.DownloadModificationDataFromRepos(modName);
            await DataHandler.ReadPatchesAndAddonsForMod(modVersion);
            DataHandler.AddModModification(modVersion);

            var mod = DataHandler.GetMods().Where(m => String.Equals(m.Name, modVersion.Name, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();

            var tempModBox = new ModificationContainer(mod);
            ModsListSource.Add(tempModBox);
            MoveModInList(tempModBox, ModsListSource.Count - 1, 0);

            ModsList.ItemsSource = ModsListSource;

            EnableUI();
        }
        #endregion

        #region MainWindowEvents

        private void ListBox_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            Point point = e.GetPosition(null);
            Vector diff = _dragStartPoint - point;
            if (!mouseOverVersionList && e.LeftButton == MouseButtonState.Pressed && (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance || Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance))
            {
                var lbi = FindVisualParent<ListBoxItem>(((DependencyObject)e.OriginalSource));
                if (lbi != null)
                {
                    DragDrop.DoDragDrop(lbi, lbi.DataContext, DragDropEffects.Move);
                }
            }
        }

        private void VersionsList_MouseEnter(object sender, MouseEventArgs e)
        {
            mouseOverVersionList = true;
        }

        private void VersionsList_MouseLeave(object sender, MouseEventArgs e)
        {
            mouseOverVersionList = false;
        }

        private void ListBoxItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _dragStartPoint = e.GetPosition(null);
        }

        private void ListBoxItem_Drop(object sender, DragEventArgs e)
        {
            if (sender is ListBoxItem)
            {
                var source = e.Data.GetData(typeof(ModificationContainer)) as ModificationContainer;
                var target = ((ListBoxItem)(sender)).DataContext as ModificationContainer;

                var sourceIndex = ModsList.Items.IndexOf(source);
                var targetIndex = ModsList.Items.IndexOf(target);
                MoveModInList(source, sourceIndex, targetIndex);
            }
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (Mouse.LeftButton == MouseButtonState.Pressed)
                this.DragMove();
        }

        private void CancelAllAddonsDownloads()
        {
            foreach (var patchData in PatchesList.Items)
            {
                var data = (ModificationContainer)patchData;
                data.CancelDownload();
            }

            foreach (var addonData in AddonsList.Items)
            {
                var data = (ModificationContainer)addonData;
                data.CancelDownload();
            }
        }

        private async void ModsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_ignoreSelectionFlagMods && e.OriginalSource is ListBox)
            {
                if (e.AddedItems.Count > 0)
                {
                    if ((DataHandler.GetSelectedMod() != null && !DataHandler.GetSelectedMod().Equals(((ModificationContainer)e.AddedItems[0]).ContainerModification)) ||
                        DataHandler.GetSelectedMod() == null)
                    {
                        //TODO dont cancel downloads
                        CancelAllAddonsDownloads();

                        ((ModificationContainer)ModsList.SelectedItem).SetUnSelectedStatus();
                        ((ModificationContainer)ModsList.SelectedItem).ContainerModification.IsSelected = false;

                        ((ModificationContainer)e.AddedItems[0]).ContainerModification.IsSelected = true;

                        DisableUI();
                        await UpdateAddonsAndPatches(((ModificationContainer)e.AddedItems[0]).ContainerModification);
                        UpdateTabs();
                        EnableUI();
                    }
                    else
                        (((ModificationContainer)e.AddedItems[0]).ContainerModification).IsSelected = true;

                    _ignoreSelectionFlagMods = true;
                    ModsList.UnselectAll();
                    ModsList.SelectedItems.Add(e.AddedItems[0]);


                    ((ModificationContainer)e.AddedItems[0]).SetSelectedStatus();

                    e.Handled = true;
                    _ignoreSelectionFlagMods = false;
                }
                else
                {
                    ((ModificationContainer)e.RemovedItems[0]).SetUnSelectedStatus();
                    ((ModificationContainer)e.RemovedItems[0]).ContainerModification.IsSelected = false;
                    PatchesButton.Visibility = Visibility.Hidden;
                    AddonsButton.Visibility = Visibility.Hidden;
                }
            }
            SetFocuses();
        }

        private void PatchesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_ignoreSelectionFlagPatches && e.OriginalSource is ListBox && ((ListBox)sender).Items.Count != 0)
            {
                if (e.AddedItems.Count > 0)
                {
                    ListBox listBox = sender as ListBox;
                    _ignoreSelectionFlagPatches = true;

                    if (DataHandler.GetSelectedPatch() == null)
                    {
                        PatchesList.SelectedItems.Add(e.AddedItems[0]);
                        var patch = ((ModificationContainer)PatchesList.SelectedItem).ContainerModification;
                        patch.IsSelected = true;
                    }
                    else
                    {
                        var selectedPatch = ((ModificationContainer)listBox.SelectedItems[0]).ContainerModification;
                        var newPatch = ((ModificationContainer)e.AddedItems[0]).ContainerModification;

                        if (!String.Equals(selectedPatch.Name, newPatch.Name, StringComparison.OrdinalIgnoreCase))
                        {
                            selectedPatch.IsSelected = false;
                            ((ModificationContainer)listBox.SelectedItems[0]).SetUnSelectedStatus();
                            ((ModificationContainer)listBox.SelectedItems[0]).ContainerModification.IsSelected = false;

                            newPatch.IsSelected = true;
                        }
                    }

                    PatchesList.UnselectAll();
                    PatchesList.SelectedItems.Add(e.AddedItems[0]);
                    ((ModificationContainer)listBox.SelectedItems[0]).SetSelectedStatus();

                    e.Handled = true;
                    _ignoreSelectionFlagPatches = false;
                }
                else
                {
                    var mod = DataHandler.GetSelectedMod();
                    var patch = ((ModificationContainer)e.RemovedItems[0]).ContainerModification;

                    if (String.Equals(patch.DependenceName, mod.Name, StringComparison.CurrentCultureIgnoreCase))
                    {
                        patch.IsSelected = false;
                        ((ModificationContainer)e.RemovedItems[0]).SetUnSelectedStatus();
                        ((ModificationContainer)e.RemovedItems[0]).ContainerModification.IsSelected = false;
                    }
                }

                UpdateAddonsList();
            }
        }

        private void AddonsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.OriginalSource is ListBox && ((ListBox)sender).Items.Count != 0)
            {
                if (e.AddedItems.Count > 0)
                {
                    var addon = ((ModificationContainer)e.AddedItems[0]).ContainerModification;
                    ((ModificationContainer)e.AddedItems[0]).SetSelectedStatus();
                    addon.IsSelected = true;
                }
                else
                {
                    var mod = DataHandler.GetSelectedMod();
                    var addon = ((ModificationContainer)e.RemovedItems[0]).ContainerModification;

                    if (String.Equals(addon.DependenceName, mod.Name, StringComparison.CurrentCultureIgnoreCase))
                    {
                        addon.IsSelected = false;
                        ((ModificationContainer)e.RemovedItems[0]).SetUnSelectedStatus();
                        ((ModificationContainer)e.RemovedItems[0]).ContainerModification.IsSelected = false;
                    }
                }
            }
        }

        private void VersionsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBoxSelectedItem = (ComboBoxData)((ComboBox)sender).SelectedItem;

            if (comboBoxSelectedItem != null)
            {
                foreach (var version in comboBoxSelectedItem.ModBoxData.ContainerModification.ModificationVersions)
                {
                    if (!String.Equals(comboBoxSelectedItem.VersionName, version.Version, StringComparison.OrdinalIgnoreCase))
                    {
                        version.IsSelected = false;
                    }
                    else
                        version.IsSelected = true;
                }
            }
        }
        
        private void GridItem_Loaded(object sender, RoutedEventArgs e)
        {
            var modGrid = sender as Grid;

            var gridControls = new GridControls(modGrid);

            var modData = modGrid.DataContext as ModificationContainer;

            if (modData != null)
            {
                modData.SetUIElements(gridControls);

                //Select mod from saved data
                if (DataHandler.GetSelectedMod() != null && String.Equals(DataHandler.GetSelectedMod().Name, modData.ContainerModification.Name, StringComparison.OrdinalIgnoreCase))
                {
                    ModsList.SelectedItem = modData;
                }

                //Select patch and addons for mod from saved data
                if (DataHandler.GetSelectedPatch() != null)
                {
                    if (String.Equals(DataHandler.GetSelectedPatch().Name, modData.ContainerModification.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        PatchesList.SelectedItem = modData;
                    }
                }

                if (DataHandler.GetSelectedAddonsForSelectedMod().Contains(modData.ContainerModification))
                    AddonsList.SelectedItems.Add(modData);
            }
            SetFocuses();
        }
        #endregion

        #region SupportMethods

        private void MoveModInList(ModificationContainer source, int sourceIndex, int targetIndex)
        {
            if (sourceIndex < targetIndex)
            {
                ModsListSource.Insert(targetIndex + 1, source);
                ModsListSource.RemoveAt(sourceIndex);
            }
            else
            {
                int removeIndex = sourceIndex + 1;
                if (ModsListSource.Count + 1 > removeIndex)
                {
                    ModsListSource.Insert(targetIndex, source);
                    ModsListSource.RemoveAt(removeIndex);
                }
            }

            SetIndexNumbersForMods();
        }

        private void SetIndexNumbersForMods()
        {
            for (var i = 0; i < ModsListSource.Count; i++)
            {
                ModsListSource[i].ContainerModification.NumberInList = i;
            }
        }

        private T FindVisualParent<T>(DependencyObject child)
    where T : DependencyObject
        {
            var parentObject = VisualTreeHelper.GetParent(child);
            if (parentObject == null)
                return null;
            T parent = parentObject as T;
            if (parent != null)
                return parent;
            return FindVisualParent<T>(parentObject);
        }

        private void DisableUI()
        {
            ModsButton.IsEnabled = false;
            PatchesButton.IsEnabled = false;
            AddonsButton.IsEnabled = false;

            ButtonOptions.IsEnabled = false;
            ButtonQuickStart.IsEnabled = false;
            ButtonWindowed.IsEnabled = false;
            ButtonStartGame.IsEnabled = false;
            ButtonWorldBuilder.IsEnabled = false;
            DownloadingImage.Visibility = Visibility.Visible;
            DownloadingImage2.Visibility = Visibility.Visible;

            AddModButton.IsEnabled = false;

            ManualAddMod.IsEnabled = false;
            ManualAddAddon.IsEnabled = false;
            ManualAddPatch.IsEnabled = false;
        }

        private void EnableUI()
        {
            ModsButton.IsEnabled = true;
            PatchesButton.IsEnabled = true;
            AddonsButton.IsEnabled = true;

            ButtonOptions.IsEnabled = true;
            ButtonQuickStart.IsEnabled = true;
            ButtonWindowed.IsEnabled = true;
            ButtonStartGame.IsEnabled = true;
            ButtonWorldBuilder.IsEnabled = true;
            DownloadingImage.Visibility = Visibility.Hidden;
            DownloadingImage2.Visibility = Visibility.Hidden;

            AddModButton.IsEnabled = true;
            ManualAddMod.IsEnabled = true;
            ManualAddAddon.IsEnabled = true;
            ManualAddPatch.IsEnabled = true;
        }

        private void HideAllLists()
        {
            ManualAddMod.Visibility = Visibility.Hidden;
            ManualAddPatch.Visibility = Visibility.Hidden;
            ManualAddAddon.Visibility = Visibility.Hidden;

            AddModButton.Visibility = Visibility.Hidden;

            ModsList.Visibility = Visibility.Hidden;
            PatchesList.Visibility = Visibility.Hidden;
            AddonsList.Visibility = Visibility.Hidden;
        }

        private async Task CheckAndUpdateGentool()
        {
            if (!DataHandler.GentoolAutoUpdate() && File.Exists("d3d8.dll"))
            {
                File.Move("d3d8.dll", "d3d8.dll" + EntryPoint.GenLauncherReplaceSuffix);
                return;
            }

            if (!await GentoolHandler.CanConnectToGentoolWebSite())
            {
                UpdateProgress.Text = "Unable to check gentool";
                return;
            }

            if (GentoolHandler.IsGentoolOutDated())
            {
                await DownloadGentool();
            }
        }

        private async Task CheckModdedExe()
        {
            if (EntryPoint.SessionInfo.GameMode == Game.Generals)
                DataHandler.SetModdedExeStatus(false);

            if (!File.Exists("modded.exe") && DataHandler.IsModdedExe())
            {
                await DownloadModdedGeneralsExe();
            }
        }

        private async Task DownloadModdedGeneralsExe()
        {
            tokenSource = new CancellationTokenSource();

            try
            {
                using (var client = new ContentDownloader(EntryPoint.ModdedExeDownloadLink, RenameDownloadedModdedGeneralsExe, string.Empty, tokenSource.Token, false))
                {
                    client.ProgressChanged += LauncherUpdateProgressChanged;
                    SetProgressBarInInstallMode();
                    await client.StartDownload();

                    SetProgressBarInPassivelMode();
                }
            }
            catch
            {
                UpdateProgressBar.Value = 0;
                UpdateProgress.Text = "Unable to download modded.exe";
            }
        }

        private async Task DownloadGentool()
        {
            if (File.Exists("d3d8.dll"))
                File.Move("d3d8.dll", "d3d8.dll" + EntryPoint.GenLauncherReplaceSuffix);

            try
            {
                using (var client = new ContentDownloader(GentoolHandler.GetGentoolDownloadLink(), null, string.Empty))
                {
                    client.ProgressChanged += LauncherUpdateProgressChanged;
                    SetProgressBarInInstallMode();
                    await client.StartDownload();

                    if (File.Exists("d3d8.dll" + EntryPoint.GenLauncherReplaceSuffix))
                        File.Delete("d3d8.dll" + EntryPoint.GenLauncherReplaceSuffix);
                    SetProgressBarInPassivelMode();
                }
            }
            catch
            {
                UpdateProgressBar.Value = 0;
                UpdateProgress.Text = "Unable to check gentool";
            }
        }

        private async Task CheckAndDowloadWB()
        {
            if (!File.Exists(EntryPoint.WorldBuilderExeName))
            {
                try
                {
                    using (var client = new ContentDownloader(EntryPoint.WorldBuilderDownloadLink, null, string.Empty))
                    {
                        client.ProgressChanged += LauncherUpdateProgressChanged;
                        SetProgressBarInInstallMode();
                        await client.StartDownload();
                        SetProgressBarInPassivelMode();
                    }
                }
                catch
                {
                    UpdateProgressBar.Value = 0;
                    UpdateProgress.Text = "Cannot download WorldbuilderNT27";
                }
            }
        }

        private static void RenameDownloadedModdedGeneralsExe(string tempFileName)
        {
            File.Move(tempFileName, "modded.exe");
        }

        private static void SelfUpdate(string tempFileName)
        {
            var BatFileName = Guid.NewGuid().ToString() + ".bat";
            var ExutetableFileName = System.IO.Path.GetFileName(Process.GetCurrentProcess().MainModule.FileName);
            var tempFile = ExutetableFileName + "Old";

            using (var SW = new StreamWriter(BatFileName))
            {
                SW.WriteLine(String.Format(
                    "ren \"{0}\" \"{3}\"  \r\n " +
                    "ren \"{1}\" \"{0}\" \r\n " +
                    "start \"\" \"{0}\" \r\n " +
                    "del \"{3}\" \r\n " +
                    "del \"{2}\"", ExutetableFileName, tempFileName, BatFileName, tempFile));
                SW.Flush();
                SW.Close();
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            Process.Start(new ProcessStartInfo() { UseShellExecute = false, FileName = BatFileName, CreateNoWindow = true });
            System.Windows.Application.Current.Shutdown();
        }

        private async Task UpdateAddonsAndPatches(ModificationReposVersion mod)
        {
            if (mod != null)
            {
                await DataHandler.ReadPatchesAndAddonsForMod(mod);
            }
        }

        private void UpdateTabs()
        {
            var currentMod = DataHandler.GetSelectedMod();

            if (currentMod != null)
            {
                PatchesButton.Content = "Patches for " + currentMod.Name;
                AddonsButton.Content = "Addons for " + currentMod.Name;

                ManualAddPatch.Content = "Add patch for " + currentMod.Name + " from files";
                ManualAddAddon.Content = "Add addon for " + currentMod.Name + " from files";

                PatchesButton.Visibility = Visibility.Visible;
                AddonsButton.Visibility = Visibility.Visible;

                UpdatePatchesList();
                UpdateAddonsList();
            }
            else
            {
                PatchesButton.Visibility = Visibility.Hidden;
                AddonsButton.Visibility = Visibility.Hidden;
            }
        }

        #region DownloadModification

        private async void DownloadMod(ModificationContainer modData)
        {
            if (string.IsNullOrEmpty(modData.LatestVersion.S3HostLink) || string.IsNullOrEmpty(modData.LatestVersion.S3BucketName))
            {
                await DownloadModBySimpleLink(modData);
            }
            else
            {
                var succes = await GetModFilesInfoFromS3StorageAndDownloadMod(modData);

                if (succes)
                    return;

                if (!String.IsNullOrEmpty(modData.LatestVersion.SimpleDownloadLink))
                    await DownloadModBySimpleLink(modData);
                else
                {                    
                    var errorMsg = modData._GridControls._InfoTextBlock.Text;
                    DownloadCrashed(modData, errorMsg);
                }
            }
        }


        private async Task DownloadModBySimpleLink(ModificationContainer modData)
        {
            modData.PrepareControlsToDownloadMode();
            var client = new ModificationDownloader(modData);

            modData.SetDownloader(client);
            client.ProgressChanged += DownloadProgressChanged;
            client.Done += ModificationDownloadDone;

            try
            {
                await client.StartSimpleDownload();
            }
            catch (Exception e)
            {
                downloadingCount -= 1;
                modData.ClearDownloader();
                modData.SetUIMessages(e.Message);
            }
        }

        private async Task<bool> GetModFilesInfoFromS3StorageAndDownloadMod(ModificationContainer modData)
        {
            modData.PrepareControlsToDownloadMode();
            modData.SetUIMessages("Creating temporary copy and checking changes...");

            List<ModificationFileInfo> filesToDownload = new List<ModificationFileInfo>();

            string tempDirectoryName;

            try
            {
                var tempVersionHandler = new TempVersionHandler();
                await tempVersionHandler.DownloadFilesInfoFromS3Storage(modData);
                tempDirectoryName = await Task.Run(() => tempVersionHandler.CreateTempCopyOfFolder());

                filesToDownload = tempVersionHandler.GetFilesToDownload();
            }
            catch (Exception e)
            {
                downloadingCount -= 1;
                modData.SetUIMessages(e.Message);
                return false;
            }

            try
            {
                return await DownloadFilesFromS3Storage(modData, filesToDownload, tempDirectoryName);
            }
            catch (Exception e)
            {
                downloadingCount -= 1;
                modData.ClearDownloader();
                modData.SetUIMessages(e.Message);
                return false;
            }
        }

        private async Task<bool> DownloadFilesFromS3Storage(ModificationContainer modData, List<ModificationFileInfo> filesToDownload, string tempDirectoryName)
        {
            var client = new ModificationDownloader(modData);
            modData.SetDownloader(client);
            client.ProgressChanged += DownloadProgressChanged;
            client.Done += ModificationDownloadDone;

            var result = await client.StartS3Download(filesToDownload, tempDirectoryName);

            if (result.Crashed && !result.Canceled)
                return false;
            else
                return true;
        }

        #endregion

        private void SetFocuses()
        {
            var container = ModsList.ItemContainerGenerator.ContainerFromItem(ModsList.SelectedItem) as FrameworkElement;
            if (container != null)
            {
                container.Focus();
            }

            container = PatchesList.ItemContainerGenerator.ContainerFromItem(PatchesList.SelectedItem) as FrameworkElement;
            if (container != null)
            {
                container.Focus();
            }

            foreach (var element in AddonsList.SelectedItems)
            {
                container = AddonsList.ItemContainerGenerator.ContainerFromItem(element) as FrameworkElement;
                if (container != null)
                {
                    container.Focus();
                }
            }
        }

        private List<ModificationVersion> GetVersionOfActiveVersions()
        {
            var versionsList = new List<ModificationVersion>();

            versionsList.Add(DataHandler.GetSelectedModVersion());
            versionsList.Add(DataHandler.GetSelectedPatchVersion());
            versionsList.AddRange(DataHandler.GetSelectedAddonsVersions());

            return versionsList.Where(m => m != null).ToList();
        }

        #endregion

        #region CheckingsBeforeGameRun

        private bool ModificationsAreInstalled()
        {
            string modMessage;

            var mainMessage = "Launch aborted";

            var selectedModVersion = DataHandler.GetSelectedModVersion();
            var selectedPatchVersion = DataHandler.GetSelectedPatchVersion();

            var selectedAddonsVersions = DataHandler.GetSelectedAddonsVersions().Where(m => m != null);

            if (selectedModVersion == null)
            {
                modMessage = "Please select installed mod, before run game!";
                CreateErrorWindow(mainMessage, modMessage);

                return false;
            }

            if (selectedModVersion != null && !selectedModVersion.Installed)
            {
                modMessage = String.Format("{0} was selected but not installed -  launch aborted!", selectedModVersion.Name);
                CreateErrorWindow(mainMessage, modMessage);
                return false;
            }

            if (selectedPatchVersion != null && !selectedPatchVersion.Installed)
            {
                modMessage = String.Format("{0} was selected but not installed -  launch aborted!", selectedPatchVersion.Name);
                CreateErrorWindow(mainMessage, modMessage);
                return false;
            }

            foreach (var addon in selectedAddonsVersions)
            {
                if (!addon.Installed)
                {
                    modMessage = String.Format("{0} was selected but not installed -  launch aborted!", addon.Name);
                    CreateErrorWindow(mainMessage, modMessage);
                    return false;
                }
            }

            return true;
        }

        private bool ModificationsAreReadyToRun()
        {
            string modMessage;

            var mainMessage = "Launch aborted";

            var selectedModVersion = DataHandler.GetSelectedModVersion();

            var selectedModData = (ModificationContainer)ModsList.SelectedItem;

            ModificationContainer selectedPatchData = null;
            if (PatchesList.SelectedItems.Count > 0)
                selectedPatchData = (ModificationContainer)PatchesList.SelectedItems[0];

            List<ModificationContainer> selectedAddonsData = new List<ModificationContainer>();
            List<ModificationContainer> selectedGAddonsData = new List<ModificationContainer>();

            foreach (var data in AddonsList.SelectedItems)
            {
                selectedAddonsData.Add((ModificationContainer)data);
            }

            if (selectedModVersion == null)
            {
                modMessage = "Please select installed mod, before run game!";
                CreateErrorWindow(mainMessage, modMessage);
                return false;
            }

            if (selectedModData != null && selectedModData.Downloader != null)
            {
                modMessage = String.Format("Cannot launch {0} - installation in progress!", selectedModVersion.Name);
                CreateErrorWindow(mainMessage, modMessage);
                return false;
            }

            if (selectedPatchData != null && selectedPatchData.Downloader != null)
            {
                modMessage = String.Format("Cannot launch {0} - installation in progress!", selectedPatchData.ContainerModification.Name);
                CreateErrorWindow(mainMessage, modMessage);
                return false;
            }

            foreach (var gAddon in selectedGAddonsData)
            {
                if (gAddon.Downloader != null)
                {
                    modMessage = String.Format("{0} was selected but not installed -  launch aborted!", gAddon.ContainerModification.Name);
                    CreateErrorWindow(mainMessage, modMessage);
                    return false;
                }
            }

            foreach (var addon in selectedAddonsData)
            {
                if (addon.Downloader != null)
                {
                    modMessage = String.Format("{0} was selected but not installed -  launch aborted!", addon.ContainerModification.Name);
                    CreateErrorWindow(mainMessage, modMessage);
                    return false;
                }
            }

            return true;
        }

        private void CreateErrorWindow(string mainMessage, string modMessage)
        {
            var infoWindow = new InfoWindow(mainMessage, modMessage) { WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen };
            infoWindow.Continue.Visibility = Visibility.Hidden;
            infoWindow.Cancel.Visibility = Visibility.Hidden;
            infoWindow.ErrorBG.Visibility = Visibility.Visible;

            infoWindow.ShowDialog();
        }

        private bool ModificationsDontNeedUpdate()
        {
            var modsMessage = "Selected mod is up-to-date";

            var succes = true;

            var lastVersion = DataHandler.GetSelectedModVersions().OrderBy(m => m).Last();

            if (!lastVersion.Installed)
            {
                succes = false;
                modsMessage = String.Format("There is uninstalled update for {0} ", lastVersion.Name);
            }

            var activePatch = DataHandler.GetSelectedPatch();
            if (activePatch != null)
            {
                var lastPatchVersion = activePatch.ModificationVersions.OrderBy(m => m).LastOrDefault();

                if (lastPatchVersion != null && !lastPatchVersion.Installed)
                {
                    succes = false;
                    modsMessage = String.Format("There is uninstalled update for {0} ", lastPatchVersion.Name);
                }
            }

            var selectedAddons = DataHandler.GetSelectedAddonsForSelectedMod()?.Where(m => m != null);

            if (selectedAddons.Count() > 0)
            {
                foreach (var selectedAddon in selectedAddons)
                {
                    var selectedAddonLastVersion = selectedAddon.ModificationVersions.OrderBy(m => m).LastOrDefault();

                    if (selectedAddonLastVersion != null && !selectedAddonLastVersion.Installed)
                    {
                        succes = false;
                        modsMessage = String.Format("There is uninstalled update for {0} ", selectedAddonLastVersion.Name);
                        break;
                    }
                }
            }

            if (!succes)
            {
                var mainMessage = "There are modifications that can be updated: ";

                var infoWindow = new InfoWindow(mainMessage, modsMessage) { WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen };
                infoWindow.Ok.Visibility = Visibility.Hidden;                

                infoWindow.ShowDialog();
                return infoWindow.GetResult();
            }

            return succes;
        }

        #endregion

        #region MainButtonsEvents

        private void UpdateButton_MouseEnter(object sender, MouseEventArgs e)
        {
            var updateButton = sender as UpdateButton;
            if (updateButton != null)
                updateButton.IsBlinking = false;
        }

        private void ModsButton_Click(object sender, RoutedEventArgs e)
        {
            HideAllLists();
            ManualAddMod.Visibility = Visibility.Visible;
            ModsList.Visibility = Visibility.Visible;
            AddModButton.Visibility = Visibility.Visible;
        }

        private void PatchesButton_Click(object sender, RoutedEventArgs e)
        {
            HideAllLists();
            PatchesList.Visibility = Visibility.Visible;
            ManualAddPatch.Visibility = Visibility.Visible;
        }

        private void AddonsButton_Click(object sender, RoutedEventArgs e)
        {
            HideAllLists();
            AddonsList.Visibility = Visibility.Visible;
            ManualAddAddon.Visibility = Visibility.Visible;
        }

        private async void ButtonWorldBuilder_Click(object sender, RoutedEventArgs e)
        {
            if (_isWBRunning)
            {
                CreateErrorWindow("Launch aborted", "World Builder is already running!");
                return;
            }

            if (!ModificationsAreReadyToRun())
            {
                return;
            }

            if (!ModificationsAreInstalled())
            {
                return;
            }

            if (ModificationsDontNeedUpdate())
            {
                if (EntryPoint.SessionInfo.Connected)
                {
                    LauncherUpdate.IsEnabled = false;
                    DisableUI();
                    await CheckAndDowloadWB();
                    EnableUI();

                    SetSelfUpdatingInfo(EntryPoint.SessionInfo.Connected);
                    UpdateProgress.Text = String.Empty;
                    UpdateProgressBar.Value = 0;
                }

                _isWBRunning = true;
                await GameLauncher.PrepareAndLaunchWorldBuilder(GetVersionOfActiveVersions());
                _isWBRunning = false;
            }

            SetFocuses();
        }

        private async void Launch_Click(object sender, RoutedEventArgs e)
        {
            if (_isGameRunning)
            {
                CreateErrorWindow("Launch aborted", "Generals game is already running!");
                return;
            }

            if (!ModificationsAreReadyToRun())
            {
                return;
            }

            if (!ModificationsAreInstalled())
            {
                return;
            }

            if (ModificationsDontNeedUpdate())
            {
                DisableUI();
                await CheckAndUpdateGentool();
                await CheckModdedExe();
                var activeVersions = GetVersionOfActiveVersions();
                _isGameRunning = true;
                EnableUI();
                await GameLauncher.PrepareAndRunGame(activeVersions);
                _isGameRunning = false;
            }

            SetFocuses();
        }

        private void LauncherUpdate_Click(object sender, RoutedEventArgs e)
        {
            if (updating)
            {
                LauncherUpdate.Content = "Update";
                UpdateProgress.Text = String.Empty;
                UpdateProgressBar.Value = 0;
                tokenSource.Cancel();
                EnableUI();
                updating = false;
            }
            else
            {
                if (downloadingCount != 0)
                    UpdateProgress.Text = "Please finish all installations";
                else
                {
                    UpdateLauncher();
                    LauncherUpdate.Content = "Cancel";
                    DisableUI();
                    updating = true;
                }
            }
        }

        private void ButtonOptions_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
            var optionsWindow = new OptionsWindow() { WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen };
            optionsWindow.ShowDialog();
            this.Show();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void ButtonWindowed_Click(object sender, RoutedEventArgs e)
        {
            DataHandler.SetWindowedStatus(!DataHandler.IsWindowed());
            UpdateWindowedStatus();
        }

        private void UpdateWindowedStatus()
        {
            if (DataHandler.IsWindowed())
                ButtonWindowed.Content = "CHANGE TO FULL SCREEN";
            else
                ButtonWindowed.Content = "CHANGE TO WINDOWED";
        }

        private void ButtonQuickStart_Click(object sender, RoutedEventArgs e)
        {
            DataHandler.SetQuickStartStatus(!DataHandler.IsQuickStart());
            UpdateQuickStartStatus();
        }

        private void UpdateQuickStartStatus()
        {
            if (DataHandler.IsQuickStart())
                ButtonQuickStart.Content = "CHANGE TO NORMAL START";
            else
                ButtonQuickStart.Content = "CHANGE TO QUICK START";
        }

        private void AddMod_Click(object sender, RoutedEventArgs e)
        {
            var addedModificationsNames = DataHandler.GetMods();
            var notAddedModificationsNames = DataHandler.ReposModsNames.Where(t => !addedModificationsNames.Select(m => m.Name.ToLower()).Contains(t.ToLower())).ToList();

            var addNewModWindow = new AddModificationWindow(notAddedModificationsNames)
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
            };

            addNewModWindow.AddModification += AddModToList;
            addNewModWindow.ShowDialog();
            SetFocuses();
        }

        #endregion

        #region ModGridUIEvents

        private void Update_Click(object sender, RoutedEventArgs e)
        {
            var modGrid = (Grid)(((Button)sender).Parent);
            var modData = (ModificationContainer)modGrid.DataContext;

            if (modData.Downloader == null)
            {                
                DownloadMod(modData);
                e.Handled = false;
            }
            else
            {
                modData.CancelDownload();
            }
        }

        static void DownloadProgressChanged(long? totalDownloadSize, long totalBytesRead, double? progressPercentage, ModificationContainer modData, string fileName)
        {
            if (progressPercentage.HasValue)
            {
                var percentage = Convert.ToInt32(progressPercentage.Value);

                string message;

                if (String.IsNullOrEmpty(fileName))
                    message = String.Format("Download in progress {0}MB / {1}MB", (totalBytesRead / 1048576).ToString(), (totalDownloadSize.Value / 1048576).ToString());
                else
                    message = String.Format("Download {0}: {1}MB / {2}MB", fileName, (totalBytesRead / 1048576).ToString(), (totalDownloadSize.Value / 1048576).ToString());

                if (percentage == 100)
                {
                    message = "Unpacking...";
                }

                modData.SetUIMessages(message, percentage);
            }
        }

        private void ModificationDownloadDone(ModificationContainer modData, DownloadResult result)
        {
            modData.ClearDownloader();

            downloadingCount -= 1;

            if (result.Canceled)
            {
                DownloadCanceled(modData);
                return;
            }

            if (result.TimedOut)
            {
                DownloadCrashed(modData, result.Message);
                return;
            }

            if (result.Crashed)
            {
                DownloadCrashed(modData, result.Message);
                return;
            }

            SuccesModDownload(modData);
        }

        private void SuccesModDownload(ModificationContainer modData)
        {
            if (DataHandler.GetAutoDeleteOldVersionsOption())
            {
                DeleteOutDatedModifications(modData);
            }

            DataHandler.UpdateModificationsData();
            modData.ContainerModification.Installed = true;

            modData.ClearDownloader();
            modData.UpdateUIelements();
            modData.SetUnactiveProgressBar();
        }

        private void DeleteOutDatedModifications(ModificationContainer modData)
        {
            var versions = modData.ContainerModification.ModificationVersions;

            foreach (var versionData in versions)
                if (versionData != modData.LatestVersion)
                    DataHandler.DeleteVersion(versionData);
        }

        private void DownloadCanceled(ModificationContainer modData)
        {
            modData.UpdateUIelements();
            modData.SetUIMessages("Download canceled");
            modData.SetUnactiveProgressBar();
        }

        private void DownloadCrashed(ModificationContainer modData, string message)
        {
            modData.UpdateUIelements();
            modData.SetUIMessages("Error: " + message);
            modData.SetUnactiveProgressBar();
        }

        private void DiscordButton_Click(object sender, RoutedEventArgs e)
        {
            var modGrid = (Grid)(((Button)sender).Parent);
            var modData = (ModificationContainer)modGrid.DataContext;

            var discordUrl = modData.ContainerModification.DiscordLink;

            if (!string.IsNullOrEmpty(discordUrl))
            {
                discordUrl = discordUrl.Replace("&", "^&");
                Process.Start(new ProcessStartInfo("cmd", $"/c start {discordUrl}") { CreateNoWindow = true });
            }
        }

        private void Change_Click(object sender, RoutedEventArgs e)
        {
            var modGrid = (Grid)(((Button)sender).Parent);
            var modData = (ModificationContainer)modGrid.DataContext;

            var newsUrl = modData.ContainerModification.NewsLink;

            if (!string.IsNullOrEmpty(newsUrl))
            {
                newsUrl = newsUrl.Replace("&", "^&");
                Process.Start(new ProcessStartInfo("cmd", $"/c start {newsUrl}") { CreateNoWindow = true });
            }
        }

        private void NetworkInfo_Click(object sender, RoutedEventArgs e)
        {
            var modGrid = (Grid)(((Button)sender).Parent);
            var modData = (ModificationContainer)modGrid.DataContext;

            var networkUrl = modData.ContainerModification.NetworkInfo;

            if (!string.IsNullOrEmpty(networkUrl))
            {
                networkUrl = networkUrl.Replace("&", "^&");
                Process.Start(new ProcessStartInfo("cmd", $"/c start {networkUrl}") { CreateNoWindow = true });
            }
        }

        private void DeleteVersion_Click(object sender, RoutedEventArgs e)
        {
            var dataGrid = (Grid)((Button)sender).Parent;
            var versionData = (ComboBoxData)dataGrid.DataContext;

            if (versionData != null)
            {
                DataHandler.DeleteVersion(versionData);
                versionData.ModBoxData.UpdateUIelements();
            }
        }

        private void ModdbButton_Click(object sender, RoutedEventArgs e)
        {
            var modGrid = (Grid)(((Button)sender).Parent);
            var modData = (ModificationContainer)modGrid.DataContext;

            var moddbUrl = modData.ContainerModification.ModDBLink;

            if (!string.IsNullOrEmpty(moddbUrl))
            {
                moddbUrl = moddbUrl.Replace("&", "^&");
                Process.Start(new ProcessStartInfo("cmd", $"/c start {moddbUrl}") { CreateNoWindow = true });
            }
        }

        #endregion

        #region ManualAddingModifications

        private void ManualAddMod_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.DefaultExt = ".big";
            dlg.Filter = "ARCHIVE (*.big,*.7z,*.zip,*.rar)|*big;*.7z;*.zip;*.rar";
            dlg.Multiselect = true;

            var result = dlg.ShowDialog();

            if (result == true)
            {
                var setNameWindow = new ManualAddMidificationWindow(dlg.FileNames.ToList()) { WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen };
                setNameWindow.CreateModCallback += CreateModificationFromFiles;
                setNameWindow.ShowDialog();
            }
        }

        private void ManualAddPatch_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.DefaultExt = ".big";
            dlg.Filter = "ARCHIVE (*.big,*.7z,*.zip,*.rar)|*big;*.7z;*.zip;*.rar";
            dlg.Multiselect = true;

            var result = dlg.ShowDialog();

            if (result == true)
            {
                var activeMod = DataHandler.GetSelectedMod();

                var setNameWindow = new ManualAddMidificationWindow(dlg.FileNames.ToList(), activeMod.Name
                    ) { WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen };
                setNameWindow.CreateAddonCallback += CreatePatchFromFiles;
                setNameWindow.ShowDialog();
            }
        }

        private void ManualAddAddon_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.DefaultExt = ".big";
            dlg.Filter = "ARCHIVE (*.big,*.7z,*.zip,*.rar)|*big;*.7z;*.zip;*.rar";
            dlg.Multiselect = true;

            var result = dlg.ShowDialog();

            if (result == true)
            {
                var activeMod = DataHandler.GetSelectedMod();

                var setNameWindow = new ManualAddMidificationWindow(dlg.FileNames.ToList(), activeMod.Name
                    ) { WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen };
                setNameWindow.CreateAddonCallback += CreateAddonFromFiles;
                setNameWindow.ShowDialog();
            }
        }

        public async void CreateModificationFromFiles(List<string> files, string modName, string version)
        {
            DisableUI();
            await Task.Run(() => ModificationsFileHandler.CreateModificationsFromFiles(files, EntryPoint.GenLauncherModsFolder + '/' + modName + '/' + version));

            DataHandler.UpdateModificationsData();
            var savedModification = DataHandler.GetMods().Where(m => String.Equals(m.Name, modName, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();

            var modData = new ModificationContainer(savedModification);
            ModsListSource.Add(modData);
            MoveModInList(modData, ModsListSource.Count - 1, 0);

            EnableUI();
        }

        public async void CreateAddonFromFiles(List<string> files, string path, string modName, string version)
        {
            DisableUI();
            await Task.Run(() => ModificationsFileHandler.CreateModificationsFromFiles(files, EntryPoint.GenLauncherModsFolder + '/' + path + '/' + EntryPoint.AddonsFolderName + '/' + modName + '/' + version));

            DataHandler.UpdateModificationsData();
            var savedModification = DataHandler.GetAddonsForSelectedMod().Where(m => String.Equals(m.Name, modName, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();

            var modData = new ModificationContainer(savedModification);
            AddonsListSource.Add(modData);

            EnableUI();
        }

        public async void CreatePatchFromFiles(List<string> files, string path, string modName, string version)
        {
            DisableUI();
            await Task.Run(() => ModificationsFileHandler.CreateModificationsFromFiles(files, EntryPoint.GenLauncherModsFolder + '/' + path + '/' + EntryPoint.PatchesFolderName + '/' + modName + '/' + version));

            DataHandler.UpdateModificationsData();
            var savedModification = DataHandler.GetPatchesForSelectedMod().Where(m => String.Equals(m.Name, modName, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();

            var modData = new ModificationContainer(savedModification);
            PatchesListSource.Add(modData);

            EnableUI();
        }

        #endregion
    }
}
