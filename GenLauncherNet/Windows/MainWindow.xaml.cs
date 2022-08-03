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

        private ObservableCollection<ModBoxData> ModListSource = new ObservableCollection<ModBoxData>();
        private ObservableCollection<ModBoxData> PatchesListSource = new ObservableCollection<ModBoxData>();
        private ObservableCollection<ModBoxData> GlobalAddonsListSource = new ObservableCollection<ModBoxData>();
        private ObservableCollection<ModBoxData> AddonsListSource = new ObservableCollection<ModBoxData>();
        private bool connected;

        private CancellationTokenSource tokenSource;
        private bool updating = false;
        private bool _ignoreSelectionFlagMods = false;
        private bool _ignoreSelectionFlagPatches = false;
        private volatile bool _isGameRunning = false;
        private volatile bool _isWBRunning = false;
        private bool _updatingLists = false;

        private volatile int downloadingCount = 0;

        public MainWindow(bool con)
        {
            connected = con;

            InitializeComponent();

            this.MouseDown += Window_MouseDown;

            this.Closing += MainWindow_Closing;

            HideAllLists();
            ModsList.Visibility = Visibility.Visible;
            ManualAddMod.Visibility = Visibility.Visible;
            AddModButton.Visibility = Visibility.Visible;

            if (!connected)
                AddModButton.Visibility = Visibility.Hidden;

            UpdateWindowedStatus();
            UpdateQuickStartStatus();
            UpdateModsList();
            UpdateGlobalAddonsList();
            UpdateTabs();

            SetSelfUpdatingInfo(connected);
        }


        private void Exit()
        {
            this.Close();
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            foreach (var patchData in PatchesList.Items)
            {
                var data = (ModBoxData)patchData;
                data.CancelDownload();
            }

            foreach (var addonData in AddonsList.Items)
            {
                var data = (ModBoxData)addonData;
                data.CancelDownload();
            }

            foreach (var modData in ModsList.Items)
            {
                var data = (ModBoxData)modData;
                data.CancelDownload();
            }

            foreach (var addonData in GlobalAddonsList.Items)
            {
                var data = (ModBoxData)addonData;
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
            PatchesListSource = new ObservableCollection<ModBoxData>();
            PatchesList.Items.Clear();

            var s = DataHandler.GetSelectedMod();
            var selectedMod = DataHandler.GetSelectedMod();            
            if (selectedMod != null)
            {
                PatchesListSource = new ObservableCollection<ModBoxData>();              

                var groupedPatchesAndVersions = DataHandler.GetPatchVersionsForModList(selectedMod.Name).GroupBy(p => p.Name.ToUpper());

                foreach (var patchAndVersion in groupedPatchesAndVersions)
                {
                    PatchesListSource.Add(new ModBoxData(patchAndVersion.OrderBy(m => m).LastOrDefault()));
                }
                PatchesList.ItemsSource = PatchesListSource;
            }
        }

        private void UpdateAddonsList()
        {
            AddonsList.ItemsSource = null;
            AddonsListSource = new ObservableCollection<ModBoxData>();
            AddonsList.Items.Clear();

            var selectedMod = DataHandler.GetSelectedMod();

            //if ((ModsList.SelectedItem != null && ((ModBoxData)ModsList.SelectedItem).ModBoxModification != previousSelectedMod) || (DataHandler.GetSelectedMod() != null && DataHandler.GetSelectedMod() != previousSelectedMod))
            if (selectedMod != null)
            {
                AddonsListSource = new ObservableCollection<ModBoxData>();

                var groupedAddonsAndVersions = DataHandler.GetAddonVersionsForModList(selectedMod.Name).GroupBy(p => p.Name.ToUpper());

                //Adding to addonsList installed addons
                foreach (var addonAndVersion in groupedAddonsAndVersions)
                {
                    AddonsListSource.Add(new ModBoxData(addonAndVersion.OrderBy(m => m).LastOrDefault()));
                }
                AddonsList.ItemsSource = AddonsListSource;
            }
        }

        private void UpdateGlobalAddonsList()
        {
            GlobalAddonsList.ItemsSource = null;
            GlobalAddonsListSource = new ObservableCollection<ModBoxData>();
            GlobalAddonsList.Items.Clear();

            GlobalAddonsListSource = new ObservableCollection<ModBoxData>();

            var groupedgAddons = DataHandler.GetFullGlobalAddonsVersionsList().GroupBy(p => p.Name.ToUpper());

            //Adding to globalAddonsList addons
            foreach (var gAddonAndVersion in groupedgAddons)
            {
                GlobalAddonsListSource.Add(new ModBoxData(gAddonAndVersion.OrderBy(m => m).LastOrDefault()));
            }

            GlobalAddonsList.ItemsSource = GlobalAddonsListSource;
        }

        private void UpdateModsList()
        {
            ModsList.ItemsSource = null;
            ModListSource = new ObservableCollection<ModBoxData>();
            ModsList.Items.Clear();

            var addedModsNames = DataHandler.GetAddedToMainWindowModifications();
            var sortedMods = DataHandler.GetFullModsVersionsList().GroupBy(p => p.Name.ToUpper()).Where(t => addedModsNames.Contains(t.Key)).OrderBy(m => m.Key);
            var modDataList = new List<ModBoxData>();

            foreach (var modsAndVersion in sortedMods)
            {
                modDataList.Add(new ModBoxData(modsAndVersion.OrderBy(m => m).LastOrDefault()));
            }

            foreach (var modData in modDataList.OrderBy(m => !m.Favorite))
            {
                ModListSource.Add(modData);
            }

            if (modDataList.Count == 0)
                AddModButton.IsBlinking = true;
            else
                ModsList.ItemsSource = ModListSource;
        }

        public async void AddMod(string modName)
        {
            DisableUI();

            var mod = await DataHandler.DownloadModificationDataFromRepos(modName);
            await DataHandler.ReadPatchesAndAddonsForMod(mod);
            var tempModBox = new ModBoxData(mod);

            DataHandler.AddAddedModification(mod.Name);
            DataHandler.TempAddedMods.Add(mod.Name);

            ModListSource.Add(tempModBox);
            ModsList.ItemsSource = ModListSource;


            EnableUI();
        }
        #endregion

        #region MainWindowEvents

        private async void ModsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_ignoreSelectionFlagMods && e.OriginalSource is ListBox && !_updatingLists)
            {
                if (e.AddedItems.Count > 0)
                {
                    if ((DataHandler.GetSelectedMod() != null && !DataHandler.GetSelectedMod().Equals(((ModBoxData)e.AddedItems[0]).ModBoxModification)) ||
                        DataHandler.GetSelectedMod() == null)
                    {
                        //TODO dont cancel downloads
                        CancelAllAddonsDownloads();

                        ((ModBoxData)ModsList.SelectedItem).SetUnSelectedStatus();

                        var newMod = ((ModBoxData)e.AddedItems[0]).ModBoxModification;
                        DataHandler.AddActiveModification(newMod);

                        DisableUI();
                        await UpdateAddonsAndPatches(newMod);
                        UpdateTabs();
                        EnableUI();
                    }
                    else
                        DataHandler.AddActiveModification(((ModBoxData)e.AddedItems[0]).ModBoxModification);

                    _ignoreSelectionFlagMods = true;
                    ModsList.UnselectAll();
                    ModsList.SelectedItems.Add(e.AddedItems[0]);


                    ((ModBoxData)e.AddedItems[0]).SetSelectedStatus();

                    e.Handled = true;
                    _ignoreSelectionFlagMods = false;                   
                }
                else
                {
                    ((ModBoxData)e.RemovedItems[0]).SetUnSelectedStatus();
                    DataHandler.AddActiveModification(null);
                    PatchesButton.Visibility = Visibility.Hidden;
                    AddonsButton.Visibility = Visibility.Hidden;
                }
            }
            SetFocuses();
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
                var data = (ModBoxData)patchData;
                data.CancelDownload();
            }

            foreach (var addonData in AddonsList.Items)
            {
                var data = (ModBoxData)addonData;
                data.CancelDownload();
            }
        }

        private void PatchesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_ignoreSelectionFlagPatches && e.OriginalSource is ListBox && !_updatingLists && ((ListBox)sender).Items.Count != 0)
            {
                if (e.AddedItems.Count > 0)
                {
                    ListBox listBox = sender as ListBox;
                    _ignoreSelectionFlagPatches = true;

                    if (DataHandler.GetSelectedPatch() == null)
                    {
                        PatchesList.SelectedItems.Add(e.AddedItems[0]);
                        var patch = ((ModBoxData)PatchesList.SelectedItem).ModBoxModification;
                        DataHandler.AddActiveModification(patch);
                    }
                    else
                    {
                        var selectedPatch = ((ModBoxData)listBox.SelectedItems[0]).ModBoxModification;
                        var newPatch = ((ModBoxData)e.AddedItems[0]).ModBoxModification;

                        if (!String.Equals(selectedPatch.Name, newPatch.Name, StringComparison.OrdinalIgnoreCase))
                        {
                            DataHandler.RemoveActiveModification(selectedPatch);
                            ((ModBoxData)listBox.SelectedItems[0]).SetUnSelectedStatus();

                            DataHandler.AddActiveModification(newPatch);
                        }
                    }

                    PatchesList.UnselectAll();
                    PatchesList.SelectedItems.Add(e.AddedItems[0]);
                    ((ModBoxData)listBox.SelectedItems[0]).SetSelectedStatus();

                    e.Handled = true;
                    _ignoreSelectionFlagPatches = false;
                }
                else
                {
                    var mod = DataHandler.GetSelectedMod();
                    var patch = ((ModBoxData)e.RemovedItems[0]).ModBoxModification;

                    if (String.Equals(patch.DependenceName, mod.Name, StringComparison.CurrentCultureIgnoreCase))
                    {
                        DataHandler.RemoveActiveModification(patch);
                        ((ModBoxData)e.RemovedItems[0]).SetUnSelectedStatus();
                    }
                }
            }
        }

        private void AddonsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.OriginalSource is ListBox && !_updatingLists && ((ListBox)sender).Items.Count != 0)
            {
                if (e.AddedItems.Count > 0)
                {
                    var addon = ((ModBoxData)e.AddedItems[0]).ModBoxModification;
                    ((ModBoxData)e.AddedItems[0]).SetSelectedStatus();
                    DataHandler.AddActiveModification(addon);
                }
                else
                {
                    var mod = DataHandler.GetSelectedMod();
                    var addon = ((ModBoxData)e.RemovedItems[0]).ModBoxModification;

                    if (String.Equals(addon.DependenceName, mod.Name, StringComparison.CurrentCultureIgnoreCase))
                    {
                        DataHandler.RemoveActiveModification(addon);
                        ((ModBoxData)e.RemovedItems[0]).SetUnSelectedStatus();
                    }
                }
            }
        }

        private void GlolbalAddonsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.OriginalSource is ListBox && !_updatingLists)
            {
                if (e.AddedItems.Count > 0)
                {
                    var addon = ((ModBoxData)e.AddedItems[0]).ModBoxModification;

                    ((ModBoxData)e.AddedItems[0]).SetSelectedStatus();

                    DataHandler.AddActiveModification(addon);
                }
                else
                {
                    var addon = ((ModBoxData)e.RemovedItems[0]).ModBoxModification;
                    DataHandler.RemoveActiveModification(addon);
                    ((ModBoxData)e.RemovedItems[0]).SetUnSelectedStatus();
                }
            }
        }

        private void VersionsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBoxSelectedItem = (ComboBoxData)((ComboBox)sender).SelectedItem;

            if (comboBoxSelectedItem != null)
            {
                DataHandler.UpdateSelectedVersionForModification(comboBoxSelectedItem);
            }
        }
        
        private void GridItem_Loaded(object sender, RoutedEventArgs e)
        {
            var modGrid = sender as Grid;

            var gridControls = new GridControls(modGrid);

            var modData = modGrid.DataContext as ModBoxData;

            if (modData != null)
            {
                modData.SetUIElements(gridControls);

                //Select mod from saved data
                if (DataHandler.GetSelectedMod() != null && String.Equals(DataHandler.GetSelectedMod().Name, modData.ModBoxModification.Name, StringComparison.OrdinalIgnoreCase))
                {
                    ModsList.SelectedItem = modData;
                }

                //Select patch and addons for mod from saved data
                if (DataHandler.GetSelectedPatch() != null)
                {
                    if (String.Equals(DataHandler.GetSelectedPatch().Name, modData.ModBoxModification.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        PatchesList.SelectedItem = modData;
                    }
                }

                if (DataHandler.GetSelectedGlobalAddons().Contains(modData.ModBoxModification))
                    GlobalAddonsList.SelectedItems.Add(modData);

                if (DataHandler.GetSelectedAddons().Contains(modData.ModBoxModification))
                    AddonsList.SelectedItems.Add(modData);
            }
            SetFocuses();
        }
        #endregion

        #region SupportMethods

        private void DisableUI()
        {
            ModsButton.IsEnabled = false;
            GlobalAddonsButton.IsEnabled = false;
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
            ManualAddGlobalAddon.IsEnabled = false;
            ManualAddAddon.IsEnabled = false;
            ManualAddPatch.IsEnabled = false;
        }

        private void EnableUI()
        {
            ModsButton.IsEnabled = true;
            GlobalAddonsButton.IsEnabled = true;
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
            ManualAddGlobalAddon.IsEnabled = true;
            ManualAddAddon.IsEnabled = true;
            ManualAddPatch.IsEnabled = true;
        }

        private void HideAllLists()
        {
            ManualAddMod.Visibility = Visibility.Hidden;
            ManualAddGlobalAddon.Visibility = Visibility.Hidden;
            ManualAddPatch.Visibility = Visibility.Hidden;
            ManualAddAddon.Visibility = Visibility.Hidden;

            AddModButton.Visibility = Visibility.Hidden;

            ModsList.Visibility = Visibility.Hidden;
            GlobalAddonsList.Visibility = Visibility.Hidden;
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
                DisableUI();
                await DownloadGentool();
                EnableUI();
            }
        }

        private async Task CheckModdedExe()
        {
            if (!File.Exists("modded.exe"))
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

            catch (TaskCanceledException)
            {
                UpdateProgressBar.Value = 0;
                UpdateProgress.Text = "Canceled";
            }

            catch (Exception ex)
            {
                UpdateProgressBar.Value = 0;
                UpdateProgress.Text = "Error" + ex.Message;
            }
        }

        private async Task DownloadGentool()
        {
            tokenSource = new CancellationTokenSource();

            if (File.Exists("d3d8.dll"))
                File.Move("d3d8.dll", "d3d8.dll" + EntryPoint.GenLauncherReplaceSuffix);

            try
            {
                using (var client = new ContentDownloader(GentoolHandler.GetGentoolDownloadLink(), null, string.Empty, tokenSource.Token))
                {
                    client.ProgressChanged += LauncherUpdateProgressChanged;
                    SetProgressBarInInstallMode();
                    await client.StartDownload();

                    if (File.Exists("d3d8.dll" + EntryPoint.GenLauncherReplaceSuffix))
                        File.Delete("d3d8.dll" + EntryPoint.GenLauncherReplaceSuffix);
                    SetProgressBarInPassivelMode();
                }
            }
            catch (TaskCanceledException)
            {
                UpdateProgressBar.Value = 0;
                UpdateProgress.Text = "Canceled";

                if (File.Exists("d3d8.dll" + EntryPoint.GenLauncherReplaceSuffix))
                    File.Move("d3d8.dll" + EntryPoint.GenLauncherReplaceSuffix, "d3d8.dll");
            }
            catch (Exception ex)
            {
                UpdateProgressBar.Value = 0;
                UpdateProgress.Text = "Error" + ex.Message;
            }
        }

        private async Task CheckAndDowloadWB()
        {
            if (!File.Exists(EntryPoint.WorldBuilderExeName))
            {
                tokenSource = new CancellationTokenSource();

                try
                {
                    using (var client = new ContentDownloader(EntryPoint.WorldBuilderDownloadLink, null, string.Empty, tokenSource.Token))
                    {
                        client.ProgressChanged += LauncherUpdateProgressChanged;
                        SetProgressBarInInstallMode();
                        await client.StartDownload();
                        SetProgressBarInPassivelMode();
                    }
                }
                catch (TaskCanceledException)
                {
                    UpdateProgressBar.Value = 0;
                    UpdateProgress.Text = "Canceled";
                }
                catch (Exception ex)
                {
                    UpdateProgressBar.Value = 0;
                    UpdateProgress.Text = "Error" + ex.Message;
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
        
        private async void DownloadMod(ModBoxData modData)
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


        private async Task DownloadModBySimpleLink(ModBoxData modData)
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

        private async Task<bool> GetModFilesInfoFromS3StorageAndDownloadMod(ModBoxData modData)
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

        private async Task<bool> DownloadFilesFromS3Storage(ModBoxData modData, List<ModificationFileInfo> filesToDownload, string tempDirectoryName)
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

            foreach (var element in GlobalAddonsList.SelectedItems)
            {
                container = GlobalAddonsList.ItemContainerGenerator.ContainerFromItem(element) as FrameworkElement;
                if (container != null)
                {
                    container.Focus();
                }
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

        private List<ModificationVersion> GetVersionOfActiveModifications()
        {
            var versionsList = new List<ModificationVersion>();

            versionsList.Add(DataHandler.GetSelectedModAndItsVersion());
            versionsList.AddRange(DataHandler.GetSelectedGlobalAddonsAndItsVersions());
            versionsList.Add(DataHandler.GetSelectedPatchAndItsVersion());
            versionsList.AddRange(DataHandler.GetSelectedAddonsAndItsVersions());

            return versionsList.Where(m => m != null).ToList();
        }

        #endregion

        #region CheckingsBeforeGameRun

        private bool ModificationsAreInstalled()
        {
            string modMessage;

            var mainMessage = "Launch aborted";

            var selectedModVersion = DataHandler.GetSelectedModAndItsVersion();
            var selectedPatchVersion = DataHandler.GetSelectedPatchAndItsVersion();

            var selectedAddonsVersions = DataHandler.GetSelectedAddonsAndItsVersions().Where(m => m != null);
            var selectedGlobalAddonsVersions = DataHandler.GetSelectedGlobalAddonsAndItsVersions().Where(m => m != null);

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

            foreach (var gAddon in selectedGlobalAddonsVersions)
            {
                if (!gAddon.Installed)
                {
                    modMessage = String.Format("{0} was selected but not installed -  launch aborted!", gAddon.Name);
                    CreateErrorWindow(mainMessage, modMessage);
                    return false;
                }
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

            var selectedModVersion = DataHandler.GetSelectedModAndItsVersion();

            var selectedModData = (ModBoxData)ModsList.SelectedItem;

            ModBoxData selectedPatchData = null;
            if (PatchesList.SelectedItems.Count > 0)
                selectedPatchData = (ModBoxData)PatchesList.SelectedItems[0];

            List<ModBoxData> selectedAddonsData = new List<ModBoxData>();
            List<ModBoxData> selectedGAddonsData = new List<ModBoxData>();

            foreach (var data in AddonsList.SelectedItems)
            {
                selectedAddonsData.Add((ModBoxData)data);
            }

            foreach (var data in GlobalAddonsList.SelectedItems)
            {
                selectedGAddonsData.Add((ModBoxData)data);
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
                modMessage = String.Format("Cannot launch {0} - installation in progress!", selectedPatchData.ModBoxModification.Name);
                CreateErrorWindow(mainMessage, modMessage);
                return false;
            }

            foreach (var gAddon in selectedGAddonsData)
            {
                if (gAddon.Downloader != null)
                {
                    modMessage = String.Format("{0} was selected but not installed -  launch aborted!", gAddon.ModBoxModification.Name);
                    CreateErrorWindow(mainMessage, modMessage);
                    return false;
                }
            }

            foreach (var addon in selectedAddonsData)
            {
                if (addon.Downloader != null)
                {
                    modMessage = String.Format("{0} was selected but not installed -  launch aborted!", addon.ModBoxModification.Name);
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

            var activeMod = DataHandler.GetSelectedMod();
            var modVersion = DataHandler.GetModVersions(activeMod).OrderBy(m => m).Last();

            if (!modVersion.Installed)
            {
                succes = false;
                modsMessage = String.Format("There is uninstalled update for {0} ", modVersion.Name);
            }

            var activePatch = DataHandler.GetSelectedPatchAndItsVersion();
            if (activePatch != null)
            {
                var lastPatchVersion = DataHandler.GetPatchVersions(activePatch).OrderBy(m => m).LastOrDefault();

                if (lastPatchVersion != null && !lastPatchVersion.Installed)
                {                 
                    succes = false;
                    modsMessage = String.Format("There is uninstalled update for {0} ", lastPatchVersion.Name);
                }
            }

            var selectedAddons = DataHandler.GetSelectedAddons()?.Where(m => m != null);

            if (selectedAddons.Count() > 0)
            {
                foreach (var selectedAddon in selectedAddons)
                {
                    var selectedAddonLastVersion = DataHandler.GetAddonVersions(selectedAddon).OrderBy(m => m).LastOrDefault();
                    if (selectedAddonLastVersion != null && !selectedAddonLastVersion.Installed)
                    {
                        succes = false;
                        modsMessage = String.Format("There is uninstalled update for {0} ", selectedAddonLastVersion.Name);
                        break;
                    }
                }
            }

            var selectedGlobalAddons = DataHandler.GetSelectedGlobalAddons().Where(m => m != null);

            if (selectedGlobalAddons.Count() > 0)
            {
                foreach (var selectedGlobalAddon in selectedGlobalAddons)
                {
                    var selectedGlobalAddonLastVersion = DataHandler.GetFullGlobalAddonsVersionsList().Where(m => String.Equals(m.Name, selectedGlobalAddon.Name, StringComparison.OrdinalIgnoreCase))?.OrderBy(m => m).LastOrDefault();
                    if (selectedGlobalAddonLastVersion != null && !selectedGlobalAddonLastVersion.Installed)
                    {
                        succes = false;
                        modsMessage = String.Format("There is uninstalled update for {0} ", selectedGlobalAddonLastVersion.Name);
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

        private void LauncherUpdate_MouseEnter(object sender, MouseEventArgs e)
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

        private void GlobalAddonsButton_Click(object sender, RoutedEventArgs e)
        {
            HideAllLists();
            GlobalAddonsList.Visibility = Visibility.Visible;
            ManualAddGlobalAddon.Visibility = Visibility.Visible;
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
                if (connected)
                {
                    LauncherUpdate.IsEnabled = false;
                    DisableUI();
                    await CheckAndDowloadWB();
                    EnableUI();

                    SetSelfUpdatingInfo(connected);
                    UpdateProgress.Text = String.Empty;
                    UpdateProgressBar.Value = 0;
                }

                _isWBRunning = true;
                await GameLauncher.PrepareAndLaunchWorldBuilder(GetVersionOfActiveModifications());
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
                await CheckAndUpdateGentool();
                await CheckModdedExe();
                var activeVersions = GetVersionOfActiveModifications();
                _isGameRunning = true;
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
                ButtonWindowed.Content = "WINDOWED";
            else
                ButtonWindowed.Content = "FULL SCREEN";
        }

        private void ButtonQuickStart_Click(object sender, RoutedEventArgs e)
        {
            DataHandler.SetQuickStartStatus(!DataHandler.IsQuickStart());
            UpdateQuickStartStatus();
        }

        private void UpdateQuickStartStatus()
        {
            if (DataHandler.IsQuickStart())
                ButtonQuickStart.Content = "QUICK START";
            else
                ButtonQuickStart.Content = "NORMAL START";
        }

        private void AddMod_Click(object sender, RoutedEventArgs e)
        {
            var addedModificationsNames = DataHandler.GetAddedToMainWindowModifications();            
            var notAddedModificationsNames = DataHandler.ReposModsNames.Where(t => !addedModificationsNames.Contains(t)).ToList(); 

            var addNewModWindow = new AddModificationWindow(notAddedModificationsNames)
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
            };

            addNewModWindow.AddModification += AddMod;
            addNewModWindow.ShowDialog();
            SetFocuses();
        }

        #endregion

        #region ModGridUIEvents

        private void MyFavorite_Click(object sender, RoutedEventArgs e)
        {
            var modGrid = (Grid)(((RadioButton)sender).Parent);
            var modData = (ModBoxData)modGrid.DataContext;

            if (!modData.Favorite)
                modData.SetFavoriteStatus();
            else
                modData.SetUnFavoriteStatus();
        }

        private void Update_Click(object sender, RoutedEventArgs e)
        {
            var modGrid = (Grid)(((Button)sender).Parent);
            var modData = (ModBoxData)modGrid.DataContext;

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

        static void DownloadProgressChanged(long? totalDownloadSize, long totalBytesRead, double? progressPercentage, ModBoxData modData, string fileName)
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

        private void ModificationDownloadDone(ModBoxData modData, DownloadResult result)
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

        private void SuccesModDownload(ModBoxData modData)
        {
            if (DataHandler.GetAutoDeleteOldVersionsOption())
            {
                DeleteOutDatedModifications(modData);
            }

            DataHandler.AddAddedModification(modData.ModBoxModification.Name);
            DataHandler.UpdateModificationsData();

            if (modData.Selected)
                DataHandler.AddActiveModification(modData.ModBoxModification);

            modData.ClearDownloader();
            modData.UpdateUIelements();
            modData.SetUnactiveProgressBar();
        }

        private void DeleteOutDatedModifications(ModBoxData modData)
        {
            var versions = modData.ModificationVersions;

            foreach (var versionData in versions)
                if (versionData != modData.LatestVersion)
                    DataHandler.DeleteVersion(versionData);
        }

        private void DownloadCanceled(ModBoxData modData)
        {
            modData.UpdateUIelements();
            modData.SetUIMessages("Download canceled");
            modData.SetUnactiveProgressBar();
        }

        private void DownloadCrashed(ModBoxData modData, string message)
        {
            modData.UpdateUIelements();
            modData.SetUIMessages("Error: " + message);
            modData.SetUnactiveProgressBar();
        }

        private void DiscordButton_Click(object sender, RoutedEventArgs e)
        {
            var modGrid = (Grid)(((Button)sender).Parent);
            var modData = (ModBoxData)modGrid.DataContext;

            var discordUrl = modData.ModBoxModification.DiscordLink;

            if (!string.IsNullOrEmpty(discordUrl))
            {
                discordUrl = discordUrl.Replace("&", "^&");
                Process.Start(new ProcessStartInfo("cmd", $"/c start {discordUrl}") { CreateNoWindow = true });
            }
        }

        private void Change_Click(object sender, RoutedEventArgs e)
        {
            var modGrid = (Grid)(((Button)sender).Parent);
            var modData = (ModBoxData)modGrid.DataContext;

            var newsUrl = modData.ModBoxModification.NewsLink;

            if (!string.IsNullOrEmpty(newsUrl))
            {
                newsUrl = newsUrl.Replace("&", "^&");
                Process.Start(new ProcessStartInfo("cmd", $"/c start {newsUrl}") { CreateNoWindow = true });
            }
        }

        private void NetworkInfo_Click(object sender, RoutedEventArgs e)
        {
            var modGrid = (Grid)(((Button)sender).Parent);
            var modData = (ModBoxData)modGrid.DataContext;

            var networkUrl = modData.ModBoxModification.NetworkInfo;

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
          
            DataHandler.DeleteVersion(versionData);

            versionData.ModBoxData.UpdateUIelements();
        }

        private void ModdbButton_Click(object sender, RoutedEventArgs e)
        {
            var modGrid = (Grid)(((Button)sender).Parent);
            var modData = (ModBoxData)modGrid.DataContext;

            var moddbUrl = modData.ModBoxModification.ModDBLink;

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

        private void ManualAddGlobalAddon_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.DefaultExt = ".big";
            dlg.Filter = "ARCHIVE (*.big,*.7z,*.zip,*.rar)|*big;*.7z;*.zip;*.rar";
            dlg.Multiselect = true;

            var result = dlg.ShowDialog();

            if (result == true)
            {
                var setNameWindow = new ManualAddMidificationWindow(dlg.FileNames.ToList()) { WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen };
                setNameWindow.CreateModCallback += CreateGAddonFromFiles;
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

            var tempModification = new ModificationReposVersion(modName);
            tempModification.Version = version;
            tempModification.ModificationType = ModificationType.Mod;
            var modData = new ModBoxData(tempModification);
            ModListSource.Add(modData);

            DataHandler.AddAddedModification(modData.ModBoxModification.Name);

            EnableUI();
        }

        public async void CreateGAddonFromFiles(List<string> files, string modName, string version)
        {
            DisableUI();
            await Task.Run(() => ModificationsFileHandler.CreateModificationsFromFiles(files, EntryPoint.GenLauncherGlobalAddonsFolder + '/' + modName + '/' + version));

            DataHandler.UpdateModificationsData();
            var tempModification = new ModificationReposVersion(modName);
            tempModification.Version = version;
            tempModification.ModificationType = ModificationType.Addon;
            var modData = new ModBoxData(tempModification);
            GlobalAddonsListSource.Add(modData);

            EnableUI();
        }

        public async void CreateAddonFromFiles(List<string> files, string path, string modName, string version)
        {
            DisableUI();
            await Task.Run(() => ModificationsFileHandler.CreateModificationsFromFiles(files, EntryPoint.GenLauncherModsFolder + '/' + path + '/' + EntryPoint.AddonsFolderName + '/' + modName + '/' + version));

            DataHandler.UpdateModificationsData();
            var tempModification = new ModificationReposVersion(modName);
            tempModification.Version = version;
            tempModification.ModificationType = ModificationType.Addon;
            tempModification.DependenceName = path;

            var modData = new ModBoxData(tempModification);
            AddonsListSource.Add(modData);

            EnableUI();
        }

        public async void CreatePatchFromFiles(List<string> files, string path, string modName, string version)
        {
            DisableUI();
            await Task.Run(() => ModificationsFileHandler.CreateModificationsFromFiles(files, EntryPoint.GenLauncherModsFolder + '/' + path + '/' + EntryPoint.PatchesFolderName + '/' + modName + '/' + version));

            DataHandler.UpdateModificationsData();
            var tempModification = new ModificationReposVersion(modName);
            tempModification.Version = version;
            tempModification.DependenceName = path;
            tempModification.ModificationType = ModificationType.Patch;
            var modData = new ModBoxData(tempModification);
            PatchesListSource.Add(modData);

            EnableUI();
        }

        #endregion        
    }
}
