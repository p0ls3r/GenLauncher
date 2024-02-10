using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using GenLauncherNet.Utility;
using Minio.Exceptions;

namespace GenLauncherNet.Windows
{
    public partial class MainWindow : Window
    {
        [DllImport("kernel32.dll")]
        private static extern bool AllocConsole();

        private ObservableCollection<ModificationViewModel> ModsListSource =
            new ObservableCollection<ModificationViewModel>();

        private ObservableCollection<ModificationViewModel> PatchesListSource =
            new ObservableCollection<ModificationViewModel>();

        private ObservableCollection<ModificationViewModel> AddonsListSource =
            new ObservableCollection<ModificationViewModel>();

        private CancellationTokenSource tokenSource;
        private bool _updating = false;
        private bool _ignoreSelectionFlagMods = false;
        private bool _ignoreSelectionFlagPatches = false;
        private volatile bool _isGameRunning = false;
        private volatile bool _isWBRunning = false;

        private volatile int downloadingCount = 0;
        private Point _dragStartPoint;

        private Window _dragdropWindow = null;
        private bool _DragAndDropDisable;

        public MainWindow()
        {
            InitializeComponent();
            SetWindowTitleBasedOnGameMode();

            this.MouseDown += Window_MouseDown;
            this.Closing += MainWindow_Closing;

            UpdateLaunchesCount();

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

            if (DataHandler.GetSelectedMod() != null)
            {
                UpdateVisualResourcesForMod(new ModificationViewModel(DataHandler.GetSelectedMod()));
            }
            else
                SetDefaultVisual();

            UpdateVisuals();

            SetSelfUpdatingInfo(EntryPoint.SessionInfo.Connected);
        }

        private void UpdateLaunchesCount()
        {
            if (DataHandler.GetLauncherCount() < 0)
                DataHandler.SetLaunchesCount(EntryPoint.LaunchesCountForUpdateAdverising);

            if (DataHandler.GetLauncherCount() > EntryPoint.LaunchesCountForUpdateAdverising)
            {
                DataHandler.SetLaunchesCount(0);

                var advertising = DataHandler.GetAdvertising();

                if (advertising != null)
                    DataHandler.DeleteModificationVersion(advertising);
            }

            DataHandler.SetLaunchesCount(DataHandler.GetLauncherCount() + 1);
        }

        #region Changing Visuals

        private void SetWindowTitleBasedOnGameMode()
        {
            if (EntryPoint.SessionInfo.GameMode == Game.ZeroHour)
            {
                this.Title += " - Command & Conquer: Generals - Zero Hour";
                return;
            }

            this.Title += " - Command & Conquer: Generals";
        }

        private void SetDefaultVisual()
        {
            EntryPoint.Colors = EntryPoint.DefaultColors;
        }

        private void UpdateVisualResourcesForMod(ModificationViewModel container)
        {
            if (container.Colors != null)
            {
                EntryPoint.Colors = container.Colors;
                return;
            }

            if (container.ContainerModification.ColorsInformation == null)
            {
                SetDefaultVisual();
                return;
            }

            var imageFileName = System.IO.Path.Combine(EntryPoint.LauncherFolder, EntryPoint.LauncherImageSubFolder,
                container.ContainerModification.Name, container.LatestVersion.Version + "bg");

            if (!File.Exists(imageFileName))
                return;

            container.Colors = new ColorsInfo(container.ContainerModification.ColorsInformation);            

            var stream = File.OpenRead(imageFileName);

            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.StreamSource = stream;
                bitmap.EndInit();
                container.Colors.GenLauncherBackgroundImage = new ImageBrush(
                    new BitmapImage(new Uri("pack://application:,,,/GenLauncher;component/Images/Background.png")));
                container.Colors.GenLauncherBackgroundImage.ImageSource = bitmap;
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

            EntryPoint.Colors = container.Colors;
        }

        private void UpdateVisuals()
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

            if (EntryPoint.Colors.GenLauncherBackgroundImage != null)
                this.Resources["GenLauncherBackGroundImage"] = EntryPoint.Colors.GenLauncherBackgroundImage;

            if (EntryPoint.SessionInfo.GameMode == Game.Generals)
                ButtonQuickStart.IsEnabled = false;

            LauncherUpdate.Refresh();

            foreach (var modData in ModsList.Items)
            {
                var data = (ModificationViewModel)modData;
                data.Refresh();
            }
        }

        #endregion

        #region Drag and Drop methods

        private void ListBox_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            Point point = e.GetPosition(null);
            Vector diff = _dragStartPoint - point;

            if (!_DragAndDropDisable && e.LeftButton == MouseButtonState.Pressed &&
                (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                 Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance))
            {
                var lbi = FindVisualParent<ListBoxItem>(((DependencyObject)e.OriginalSource));
                if (lbi != null)
                {
                    _DragAndDropDisable = true;
                    var container = lbi.DataContext as ModificationViewModel;

                    if (!ModsList.SelectedItems.Contains(container))
                        ModsList.SelectedItems.Add(container);

                    container.SetDragAndDropMod();
                    CreateDragDropWindow(lbi);
                    var result = DragDrop.DoDragDrop(lbi, lbi.DataContext, DragDropEffects.Move);

                    TerminateDragDropWindow();

                    container.RemoveDragAndDropMod();

                    if (result == DragDropEffects.Move && !ModsList.SelectedItems.Contains(container))
                        ModsList.SelectedItems.Add(container);
                    else 
                        container.SetSelectedStatus();

                    _DragAndDropDisable = false;
                }
            }
        }

        private void ModsList_DragOver(object sender, System.Windows.DragEventArgs e)
        {
            ListBox li = sender as ListBox;
            ScrollViewer sv = FindVisualChild<ScrollViewer>(ModsList);

            double tolerance = 40;
            double verticalPos = e.GetPosition(li).Y;
            double offset = 15;

            if (verticalPos < tolerance)
            {
                sv.ScrollToVerticalOffset(sv.VerticalOffset - offset);
            }
            else if (verticalPos > li.ActualHeight - tolerance)
            {
                sv.ScrollToVerticalOffset(sv.VerticalOffset + offset);
            }
        }

        public static childItem FindVisualChild<childItem>(DependencyObject obj) where childItem : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);

                if (child != null && child is childItem)
                    return (childItem)child;
                else
                {
                    childItem childOfChild = FindVisualChild<childItem>(child);

                    if (childOfChild != null)
                        return childOfChild;
                }
            }

            return null;
        }

        private void BlockDragAndDrop(object sender, MouseEventArgs e)
        {
            _DragAndDropDisable = true;
        }

        private void EnableDragAndDrop(object sender, MouseEventArgs e)
        {
            _DragAndDropDisable = false;
        }

        private void ListBoxItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _dragStartPoint = e.GetPosition(null);
        }

        private void ListBoxItem_Drop(object sender, DragEventArgs e)
        {
            if (sender is ListBoxItem)
            {
                var source = e.Data.GetData(typeof(ModificationViewModel)) as ModificationViewModel;
                var target = ((ListBoxItem)(sender)).DataContext as ModificationViewModel;

                var sourceIndex = ModsList.Items.IndexOf(source);
                var targetIndex = ModsList.Items.IndexOf(target);
                if (sourceIndex != targetIndex)
                    MoveModInList(source, sourceIndex, targetIndex);

                TerminateDragDropWindow();
            }
        }

        private void TerminateDragDropWindow()
        {
            if (this._dragdropWindow != null)
            {
                this._dragdropWindow.Close();
                this._dragdropWindow = null;
            }
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (Mouse.LeftButton == MouseButtonState.Pressed)
                this.DragMove();
        }

        private void CreateDragDropWindow(Visual dragElement)
        {
            this._dragdropWindow = new Window();
            _dragdropWindow.WindowStyle = WindowStyle.None;
            _dragdropWindow.AllowsTransparency = true;
            _dragdropWindow.AllowDrop = false;
            _dragdropWindow.Background = null;
            _dragdropWindow.IsHitTestVisible = false;
            _dragdropWindow.SizeToContent = SizeToContent.WidthAndHeight;
            _dragdropWindow.Topmost = true;
            _dragdropWindow.ShowInTaskbar = false;

            Rectangle r = new Rectangle();
            r.Width = ((FrameworkElement)dragElement).ActualWidth;
            r.Height = ((FrameworkElement)dragElement).ActualHeight;
            r.Fill = new VisualBrush(dragElement);
            this._dragdropWindow.Content = r;

            Win32Point w32Mouse = new Win32Point();
            GetCursorPos(ref w32Mouse);

            this._dragdropWindow.Left = w32Mouse.X + 1;
            this._dragdropWindow.Top = w32Mouse.Y + 1;

            this._dragdropWindow.Show();
        }

        private void ModsList_GiveFeedback(object sender, GiveFeedbackEventArgs e)
        {
            Win32Point w32Mouse = new Win32Point();
            GetCursorPos(ref w32Mouse);

            this._dragdropWindow.Left = w32Mouse.X + 1;
            this._dragdropWindow.Top = w32Mouse.Y + 1;
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetCursorPos(ref Win32Point pt);

        [StructLayout(LayoutKind.Sequential)]
        internal struct Win32Point
        {
            public Int32 X;
            public Int32 Y;
        };

        #endregion

        #region SelfUpdater

        private bool IsCurrentVersionOutDated()
        {
            var currentVersionString =
                new string(EntryPoint.Version.ToCharArray().Where(n => n >= '0' && n <= '9').ToArray());
            var latestVersionString =
                new string(DataHandler.Version.ToCharArray().Where(n => n >= '0' && n <= '9').ToArray());

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
                UpdateProgress.Text = LocalizedStrings.Instance["Unpacking"];
            else
            {
                UpdateProgressBar.Value = progressPercentage;
                UpdateProgress.Text = String.Format("{0}MB / {1}MB", (totalBytesRead / 1048576).ToString(),
                    (totalSize / 1048576).ToString());
            }
        }

        private void SetSelfUpdatingInfo(bool connected)
        {
            LauncherVersion.Text = LocalizedStrings.Instance["CurrentVersion"] + EntryPoint.Version;
            LatestLauncherVersion.Text = LocalizedStrings.Instance["NoConnection"];

            if (connected)
            {
                LatestLauncherVersion.Text = LocalizedStrings.Instance["LatestVersion"] + DataHandler.Version;
                if (IsCurrentVersionOutDated())
                {
                    LauncherUpdate.IsEnabled = true;
                    LauncherUpdate.IsBlinking = true;

                    var infoWindow = new UpdateAvailable(DataHandler.Version)
                        { WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen };

                    infoWindow.ShowDialog();
                }
            }
        }

        private async void UpdateLauncher()
        {
            tokenSource = new CancellationTokenSource();

            try
            {
                using (var client = new ContentDownloader(DataHandler.DownloadLink, SelfUpdate, "GenLauncherTemp",
                           tokenSource.Token))
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
                return;
            }
            catch (Exception e)
            {
                UpdateProgressBar.Value = 0;
                UpdateProgress.Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString("White");
                UpdateProgress.Text = LocalizedStrings.Instance["Error"] + e.Message;
                SetProgressBarInPassivelMode();
                return;
            }            
        }

        private void SetProgressBarInInstallMode()
        {
            UpdateProgress.Foreground = EntryPoint.Colors.GenLauncherDownloadTextColor;
            UpdateProgressBar.Background = new SolidColorBrush(EntryPoint.Colors.GenLauncherButtonSelectionColor);
            UpdateProgressBar.BorderBrush = EntryPoint.Colors.GenLauncherBorderColor;
        }

        private void SetProgressBarInPassivelMode()
        {
            UpdateProgressBar.Background = EntryPoint.Colors.GenLauncherDarkBackGround;
            UpdateProgressBar.BorderBrush = EntryPoint.Colors.GenLauncherInactiveBorder;
            UpdateProgress.Foreground = EntryPoint.Colors.GenLauncherDefaultTextColor;

            UpdateProgressBar.Value = 0;
            UpdateProgress.Text = String.Empty;
        }

        #endregion

        #region ListsContentFillers

        private void UpdateModsList()
        {
            ModsList.ItemsSource = null;
            ModsListSource = new ObservableCollection<ModificationViewModel>();
            ModsList.Items.Clear();

            var mods = DataHandler.GetMods().OrderBy(m => m.NumberInList).ToList();

            if (mods.Where(m => m.ModificationType == ModificationType.Advertising).Count() == 0 && mods.Count >= 3 &&
                EntryPoint.SessionInfo.Connected)
            {
                var advertising = DataHandler.GetAdvertising();
                if (advertising != null)
                {
                    DataHandler.AddModModification(advertising);
                    var advInData = DataHandler.GetMods()
                        .Where(m => String.Equals(m.Name, advertising.Name, StringComparison.OrdinalIgnoreCase))
                        .FirstOrDefault();
                    var tempModBox = new ModificationViewModel(advInData);
                    ModsListSource.Add(tempModBox);
                }
            }
            else if (mods.Where(m => m.ModificationType == ModificationType.Advertising).Count() != 0 &&
                     EntryPoint.SessionInfo.Connected)
            {
                var advertising = DataHandler.GetAdvertising();
                if (advertising != null)
                {
                    DataHandler.AddModModification(advertising);
                }
            }

            if (mods.Count > 0)
            {
                foreach (var mod in mods)
                {
                    ModsListSource.Add(new ModificationViewModel(mod));
                }
            }
            else
                AddModButton.IsBlinking = true;

            ModsList.ItemsSource = ModsListSource;
            SetIndexNumbersForMods();
        }

        private void UpdatePatchesList()
        {
            PatchesList.ItemsSource = null;
            PatchesListSource = new ObservableCollection<ModificationViewModel>();
            PatchesList.Items.Clear();

            var patches = DataHandler.GetPatchesForSelectedMod();

            if (patches.Count > 0)
            {
                PatchesListSource = new ObservableCollection<ModificationViewModel>();

                foreach (var patch in patches)
                {
                    PatchesListSource.Add(new ModificationViewModel(patch));
                }
            }

            PatchesList.ItemsSource = PatchesListSource;
        }

        private void UpdateAddonsList()
        {
            AddonsList.ItemsSource = null;
            AddonsListSource = new ObservableCollection<ModificationViewModel>();
            AddonsList.Items.Clear();

            var addons = DataHandler.GetAddonsForSelectedMod();

            if (addons.Count > 0)
            {
                AddonsListSource = new ObservableCollection<ModificationViewModel>();

                foreach (var addon in addons)
                {
                    AddonsListSource.Add(new ModificationViewModel(addon));
                }
            }

            AddonsList.ItemsSource = AddonsListSource;
        }

        public async void AddModToList(string modName)
        {
            DisableUI();

            var modVersion = await DataHandler.DownloadModificationDataFromRepos(modName);
            await DataHandler.ReadPatchesAndAddonsForMod(modVersion);
            DataHandler.AddModModification(modVersion);

            var mod = DataHandler.GetMods()
                .Where(m => String.Equals(m.Name, modVersion.Name, StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault();

            var tempModBox = new ModificationViewModel(mod);
            ModsListSource.Add(tempModBox);
            MoveModInList(tempModBox, ModsListSource.Count - 1, 0);

            ModsList.ItemsSource = ModsListSource;

            EnableUI();
        }

        #endregion

        #region SelectionsChanges

        private async void ModsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_ignoreSelectionFlagMods && System.Windows.Input.Mouse.RightButton == MouseButtonState.Pressed && e.OriginalSource is ListBox)
            {
                if (DataHandler.GetSelectedMod() == null)
                {
                    _ignoreSelectionFlagMods = true;
                    ModsList.SelectedItems.Clear();
                    _ignoreSelectionFlagMods = false;
                }
                else
                {
                    if (e.AddedItems.Count > 0)
                    {
                        _ignoreSelectionFlagMods = true;
                        ModsList.SelectedItems.Remove(e.AddedItems[0]);
                        _ignoreSelectionFlagMods = false;
                    }
                    else
                    {
                        _ignoreSelectionFlagMods = true;
                        ModsList.SelectedItems.Add(e.RemovedItems[0]);
                        _ignoreSelectionFlagMods = false;
                    }
                }

                e.Handled = true;
                return;
            }

            if (!_ignoreSelectionFlagMods && e.OriginalSource is ListBox)
            {
                if (e.AddedItems.Count > 0)
                {
                    if ((DataHandler.GetSelectedMod() != null && !DataHandler.GetSelectedMod()
                            .Equals(((ModificationViewModel)e.AddedItems[0]).ContainerModification)) ||
                        DataHandler.GetSelectedMod() == null)
                    {
                        //TODO dont cancel downloads
                        CancelAllAddonsDownloads();

                        ((ModificationViewModel)ModsList.SelectedItem).SetUnSelectedStatus();
                        ((ModificationViewModel)ModsList.SelectedItem).ContainerModification.IsSelected = false;
                        DataHandler.UnselectAllModifications();

                        ((ModificationViewModel)e.AddedItems[0]).SetSelectedStatus();
                        ((ModificationViewModel)e.AddedItems[0]).ContainerModification.IsSelected = true;

                        DisableUI();
                        await UpdateAddonsAndPatches(((ModificationViewModel)e.AddedItems[0]).ContainerModification);

                        UpdateTabs();
                        EnableUI();
                    }
                    else
                        (((ModificationViewModel)e.AddedItems[0]).ContainerModification).IsSelected = true;

                    _ignoreSelectionFlagMods = true;
                    ModsList.UnselectAll();
                    ModsList.SelectedItems.Add(e.AddedItems[0]);

                    UpdateVisualResourcesForMod((ModificationViewModel)ModsList.SelectedItem);
                    UpdateVisuals();

                    ((ModificationViewModel)e.AddedItems[0]).SetSelectedStatus();
                    ((ModificationViewModel)e.AddedItems[0]).ContainerModification.IsSelected = true;

                    e.Handled = true;
                    _ignoreSelectionFlagMods = false;
                }
                else
                {
                    ((ModificationViewModel)e.RemovedItems[0]).SetUnSelectedStatus();
                    DataHandler.UnselectAllModifications();
                    UpdateTabs();
                }
            }

            SetFocuses();
        }

        private void PatchesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!(e.OriginalSource is ListBox))
                return;

            var listBox = e.OriginalSource as ListBox;

            if (!string.Equals(listBox.Name, "PatchesList"))
                return;

            if (!_ignoreSelectionFlagMods && System.Windows.Input.Mouse.RightButton == MouseButtonState.Pressed)
            {
                if (DataHandler.GetSelectedPatch() == null)
                {
                    _ignoreSelectionFlagMods = true;
                    PatchesList.SelectedItems.Clear();
                    _ignoreSelectionFlagMods = false;
                }
                else
                {
                    if (e.AddedItems.Count > 0)
                    {
                        _ignoreSelectionFlagMods = true;
                        PatchesList.SelectedItems.Remove(e.AddedItems[0]);
                        _ignoreSelectionFlagMods = false;
                    }
                    else
                    {
                        _ignoreSelectionFlagMods = true;
                        PatchesList.SelectedItems.Add(e.RemovedItems[0]);
                        _ignoreSelectionFlagMods = false;
                    }
                }

                e.Handled = true;
                return;
            }


            if (!_ignoreSelectionFlagPatches && ((ListBox)sender).Items.Count != 0)
            {
                if (e.AddedItems.Count > 0)
                {
                    _ignoreSelectionFlagPatches = true;

                    var selectedPatch = ((ModificationViewModel)listBox.SelectedItems[0]);
                    var newPatch = ((ModificationViewModel)e.AddedItems[0]);

                    if (DataHandler.GetSelectedPatch() == null)
                    {
                        PatchesList.SelectedItems.Add(e.AddedItems[0]);
                        var patch = ((ModificationViewModel)PatchesList.SelectedItem).ContainerModification;
                        patch.IsSelected = true;
                    }
                    else
                    {
                        if (!String.Equals(selectedPatch.ContainerModification.Name, newPatch.ContainerModification.Name, StringComparison.OrdinalIgnoreCase))
                        {
                            ((ModificationViewModel)listBox.SelectedItems[0]).SetUnSelectedStatus();
                            ((ModificationViewModel)listBox.SelectedItems[0]).ContainerModification.IsSelected = false;
                            ((ModificationViewModel)e.AddedItems[0]).ContainerModification.IsSelected = true;
                        }
                    }

                    PatchesList.SelectedItems.Clear();

                    PatchesList.SelectedItems.Add(e.AddedItems[0]);

                    ((ModificationViewModel)listBox.SelectedItems[0]).SetSelectedStatus();
                    ((ModificationViewModel)listBox.SelectedItems[0]).ContainerModification.IsSelected = true;

                    e.Handled = true;
                    _ignoreSelectionFlagPatches = false;
                }
                else
                {
                    ((ModificationViewModel)e.RemovedItems[0]).SetUnSelectedStatus();
                    ((ModificationViewModel)e.RemovedItems[0]).ContainerModification.IsSelected = false;
                }

                if (System.Windows.Input.Mouse.LeftButton == MouseButtonState.Pressed)
                    UpdateAddonsList();
            }
        }

        private void AddonsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!(e.OriginalSource is ListBox))
                return;

            var listBox = e.OriginalSource as ListBox;

            if (!string.Equals(listBox.Name, "AddonsList"))
                return;

            if (!_ignoreSelectionFlagMods && Mouse.RightButton == MouseButtonState.Pressed)
            {
                if (DataHandler.GetSelectedAddonsForSelectedMod().Count == 0)
                {
                    _ignoreSelectionFlagMods = true;
                    AddonsList.SelectedItems.Clear();
                    _ignoreSelectionFlagMods = false;
                }
                else
                {
                    if (e.AddedItems.Count > 0)
                    {
                        _ignoreSelectionFlagMods = true;
                        AddonsList.SelectedItems.Remove(e.AddedItems[0]);
                        _ignoreSelectionFlagMods = false;
                    }
                    else
                    {
                        _ignoreSelectionFlagMods = true;
                        AddonsList.SelectedItems.Add(e.RemovedItems[0]);
                        _ignoreSelectionFlagMods = false;
                    }
                }

                e.Handled = true;
                return;
            }


            if (((ListBox)sender).Items.Count != 0)
            {
                if (e.AddedItems.Count > 0)
                {
                    var addon = ((ModificationViewModel)e.AddedItems[0]).ContainerModification;
                    ((ModificationViewModel)e.AddedItems[0]).SetSelectedStatus();
                    addon.IsSelected = true;
                }
                else
                {
                    ((ModificationViewModel)e.RemovedItems[0]).SetUnSelectedStatus();
                    ((ModificationViewModel)e.RemovedItems[0]).ContainerModification.IsSelected = false;
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
                    if (!String.Equals(comboBoxSelectedItem.VersionName, version.Version,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        version.IsSelected = false;
                    }
                    else
                        version.IsSelected = true;
                }
            }
        }
        #endregion

        #region MainWindowEvents

        private void Exit()
        {
            DataHandler.SetLaunchesCount(EntryPoint.LaunchesCountForUpdateAdverising);
            this.Close();
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            foreach (var patchData in PatchesList.Items)
            {
                var data = (ModificationViewModel)patchData;
                data.BruteCancelDownload();
            }

            foreach (var addonData in AddonsList.Items)
            {
                var data = (ModificationViewModel)addonData;
                data.BruteCancelDownload();
            }

            foreach (var modData in ModsList.Items)
            {
                var data = (ModificationViewModel)modData;
                data.BruteCancelDownload();
            }

            if (ModsList.Items.Count > 0)
            {
                DataHandler.SaveLauncherData();
            }
        }

        private void CancelAllAddonsDownloads()
        {
            foreach (var patchData in PatchesList.Items)
            {
                var data = (ModificationViewModel)patchData;
                data.CancelDownload();
            }

            foreach (var addonData in AddonsList.Items)
            {
                var data = (ModificationViewModel)addonData;
                data.CancelDownload();
            }
        }

        private void GridItem_Loaded(object sender, RoutedEventArgs e)
        {
            var modGrid = sender as Grid;

            var gridControls = new GridControls(modGrid);

            var modData = modGrid.DataContext as ModificationViewModel;

            if (modData != null)
            {
                modData.SetUIElements(gridControls);

                //Select mod from saved data
                if (DataHandler.GetSelectedMod() != null && String.Equals(DataHandler.GetSelectedMod().Name,
                        modData.ContainerModification.Name, StringComparison.OrdinalIgnoreCase))
                {
                    ModsList.SelectedItem = modData;
                }

                //Select patch and addons for mod from saved data
                if (DataHandler.GetSelectedPatch() != null)
                {
                    if (String.Equals(DataHandler.GetSelectedPatch().Name, modData.ContainerModification.Name,
                            StringComparison.OrdinalIgnoreCase))
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

        #region DownloadModification

        private async void DownloadMod(ModificationViewModel modData)
        {
            downloadingCount += 1;
            modData.SetActiveProgressBar();

            var factory = new UpdaterFactory();
            var updater = factory.CreateUpdater(modData);
            modData.PrepareControlsToDownloadMode();
            modData.SetUIMessages(LocalizedStrings.Instance["Preparing"]);
            modData.SetDownloader(updater);
            updater.ProgressChanged += DownloadProgressChanged;            

            try
            {
                var rdy = updater.GetDownloadReadiness();

                if (rdy.ReadyToDownload)
                {
                    updater.Done += ModificationDownloadDone;
                    modData._GridControls._UpdateButton.IsEnabled = true;
                    await updater.StartDownloadModification();
                } else
                {
                    if (rdy.Error == ErrorType.TimeOutOfSync && !IsTimeSynchronized(modData))
                    {
                        return;
                    }
                }
            }
            catch (Exception e)
            {
                downloadingCount -= 1;
                modData.ClearDownloader();
                modData.SetUIMessages(e.Message);
                modData._GridControls._UpdateButton.IsEnabled = true;
            }
        }

        private bool IsTimeSynchronized(ModificationViewModel modData)
        {
            var mainMessage = LocalizedStrings.Instance["OutOfSync"];
            var secondaryMessage = LocalizedStrings.Instance["SyncTime"];

            var infoWindow = new InfoWindow(mainMessage, secondaryMessage)
            { WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen };
            infoWindow.Ok.Visibility = Visibility.Hidden;
            infoWindow.Continue.Content = LocalizedStrings.Instance["SyncNow"];
            infoWindow.WarningPolygon1.Visibility = Visibility.Visible;
            infoWindow.WarningPolygon2.Visibility = Visibility.Visible;
            infoWindow.WarningPolygon3.Visibility = Visibility.Visible;

            infoWindow.ShowDialog();
            if (!infoWindow.GetResult())
            {
                downloadingCount -= 1;
                modData.ClearDownloader();
                DownloadCrashed(modData, LocalizedStrings.Instance["OutOfSyncCantUpdate"]); 
                modData._GridControls._UpdateButton.IsEnabled = true;
                return false;
            }

            TimeUtility.SyncSystemDateTimeWithWorldTime();

            if (TimeUtility.IsSysTimeOutOfSync())
            {
                var errorWindow = new InfoWindow(LocalizedStrings.Instance["CantSync"], LocalizedStrings.Instance["SyncManually"])
                { WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen };

                errorWindow.ErrorPolygon1.Visibility = Visibility.Visible;
                errorWindow.ErrorPolygon2.Visibility = Visibility.Visible;
                errorWindow.Continue.Visibility = Visibility.Hidden;
                errorWindow.Cancel.Visibility = Visibility.Hidden;

                errorWindow.ShowDialog();
                downloadingCount -= 1;
                modData.ClearDownloader();
                DownloadCrashed(modData, LocalizedStrings.Instance["OutOfSyncCantUpdate"]);
                modData._GridControls._UpdateButton.IsEnabled = true;
                return false;
            }

            return true;
        }

        #endregion

        #region SupportMethods

        private void ApplyDefaultOptions()
        {
            var gameOptionsHandler = new GameOptionsHandler();
            gameOptionsHandler.ApplyDefaultGameOptions();

            DataHandler.SetModdedExeStatus(true);
            DataHandler.SetCameraHeight(0);
            DataHandler.SetGentoolAutoUpdateStatus(true);
            DataHandler.SetCheckModFiles(true);
            DataHandler.SetAskBeforeCheck(true);
            DataHandler.SetModdedExeStatus(true);

            DataHandler.SetWindowedStatus(true);
            GentoolHandler.SetRecommendedWindoweOptions();

            UpdateWindowedStatus();

            DataHandler.UseVulkan = false;
            DataHandler.FirstRun = false;
        }

        private void ModIncorrectInstallationNotify()
        {
            var infoWindow = new InfoWindow(LocalizedStrings.Instance["FilesCorrupted"], LocalizedStrings.Instance["Reinstall"])
            { WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen };
            infoWindow.Continue.Visibility = Visibility.Hidden;
            infoWindow.Cancel.Visibility = Visibility.Hidden;
            infoWindow.ErrorPolygon1.Visibility = Visibility.Visible;
            infoWindow.ErrorPolygon2.Visibility = Visibility.Visible;
            infoWindow.ModsMessage.FontSize = 15;

            infoWindow.ShowDialog();
        }

        private void MoveModInList(ModificationViewModel source, int sourceIndex, int targetIndex)
        {
            MoveModificationInList(ModsListSource, source, sourceIndex, targetIndex);

            SetIndexNumbersForMods();
        }

        private void MoveModificationInList(ObservableCollection<ModificationViewModel> list, ModificationViewModel source, int sourceIndex, int targetIndex)
        {
            if (sourceIndex < targetIndex)
            {
                list.Insert(targetIndex + 1, source);
                list.RemoveAt(sourceIndex);
            }
            else
            {
                int removeIndex = sourceIndex + 1;
                if (list.Count + 1 > removeIndex)
                {
                    list.Insert(targetIndex, source);
                    list.RemoveAt(removeIndex);
                }
            }
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

            if (EntryPoint.SessionInfo.GameMode == Game.ZeroHour)
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
                UpdateProgress.Text = LocalizedStrings.Instance["CantCheckGentool"];

                return;
            }

            if (GentoolHandler.IsGentoolOutDated())
            {
                await DownloadGentool();
            }
        }

        private async Task CheckAndUpdateVulkan()
        {
            if (!DataHandler.UseVulkan || !EntryPoint.SessionInfo.Connected)
            {
                return;
            }

            if (Directory.Exists(EntryPoint.VulkanDllsFolderName))
                Directory.CreateDirectory(EntryPoint.VulkanDllsFolderName);

            if (VulkanDllsHandler.IsCurrentVersionOutDated())
            {
                await DownloadVulkan();
            }

            VulkanDllsHandler.CreateVulkanSymbLinks();
        }

        private async Task DownloadVulkan()
        {
            try
            {
                using (var client = new ContentDownloader(DataHandler.VulkanData.DownloadLink, null, null, null, true, EntryPoint.VulkanDllsFolderName))
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
                UpdateProgress.Text = LocalizedStrings.Instance["CantCheckGentool"];
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
                using (var client = new ContentDownloader(EntryPoint.ModdedExeDownloadLink,
                           RenameDownloadedModdedGeneralsExe, string.Empty, tokenSource.Token, false))
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
                UpdateProgress.Text = LocalizedStrings.Instance["CantCheckGentool"];
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
                UpdateProgress.Text = LocalizedStrings.Instance["CantCheckGentool"];
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
                    UpdateProgress.Text = LocalizedStrings.Instance["CantDownloadWB"];
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

            Process.Start(new ProcessStartInfo()
                { UseShellExecute = false, FileName = BatFileName, CreateNoWindow = true });
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
                if (currentMod.ModificationType == ModificationType.Advertising)
                {
                    PatchesButton.Visibility = Visibility.Hidden;
                    AddonsButton.Visibility = Visibility.Hidden;
                    return;
                }

                PatchesButton.Content = LocalizedStrings.Instance["Patches"] + currentMod.Name;
                AddonsButton.Content = LocalizedStrings.Instance["Addons"] + currentMod.Name;

                ManualAddPatch.Content = String.Format(LocalizedStrings.Instance["AddPatchFromFiles"], currentMod.Name);
                ManualAddAddon.Content = String.Format(LocalizedStrings.Instance["AddAddonFromFiles"], currentMod.Name);

                PatchesButton.Visibility = Visibility.Visible;
                AddonsButton.Visibility = Visibility.Visible;

                UpdatePatchesList();
                UpdateAddonsList();
            }
            else
            {
                var gameName = "Generals Zero Hour";
                if (EntryPoint.SessionInfo.GameMode == Game.Generals)
                    gameName = "Generals";

                PatchesButton.Content = LocalizedStrings.Instance["Patches"] + gameName;
                AddonsButton.Content = LocalizedStrings.Instance["Addons"] + gameName;

                ManualAddPatch.Content = String.Format(LocalizedStrings.Instance["AddPatchFromFiles"], gameName);
                ManualAddAddon.Content = String.Format(LocalizedStrings.Instance["AddAddonFromFiles"], gameName);

                PatchesButton.Visibility = Visibility.Visible;
                AddonsButton.Visibility = Visibility.Visible;

                UpdatePatchesList();
                UpdateAddonsList();
            }
        }

        private void SetFocuses()
        {
            var container =
                ModsList.ItemContainerGenerator.ContainerFromItem(ModsList.SelectedItem) as FrameworkElement;
            if (container != null)
            {
                container.Focus();
            }

            container =
                PatchesList.ItemContainerGenerator.ContainerFromItem(PatchesList.SelectedItem) as FrameworkElement;
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

        private List<ModificationVersion> GetSelectedVersionsOfAllSelectedModifications()
        {
            var versionsList = new List<ModificationVersion>();

            versionsList.Add(DataHandler.GetSelectedModVersion());
            versionsList.Add(DataHandler.GetSelectedPatchVersion());
            versionsList.AddRange(DataHandler.GetSelectedAddonsVersions());

            return versionsList.Where(m => m != null).ToList();
        }

        #endregion

        #region CheckingsBeforeGameRun

        private bool NeedToApplyDefaultOptions()
        {
            var firstRun = DataHandler.FirstRun;

            if (firstRun)
            {
                var mainMessage = LocalizedStrings.Instance["FirstRun"];

                var infoWindow = new InfoWindow(mainMessage, LocalizedStrings.Instance["ApplyDefaultSettings"])
                { WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen };
                infoWindow.Ok.Visibility = Visibility.Hidden;

                infoWindow.Continue.Content = LocalizedStrings.Instance["Set default options"];
                infoWindow.Cancel.Content = LocalizedStrings.Instance["No"];

                infoWindow.WarningPolygon1.Visibility = Visibility.Visible;
                infoWindow.WarningPolygon2.Visibility = Visibility.Visible;
                infoWindow.WarningPolygon3.Visibility = Visibility.Visible;

                infoWindow.ShowDialog();
                return infoWindow.GetResult();
            }

            return false;
        }

        private bool ModificationsAreInstalled()
        {
            string modMessage;

            var mainMessage = LocalizedStrings.Instance["LaunchAborted"];

            var selectedModVersion = DataHandler.GetSelectedModVersion();
            var selectedPatchVersion = DataHandler.GetSelectedPatchVersion();
            var selectedAddonsVersions = DataHandler.GetSelectedAddonsVersions().Where(m => m != null);

            /*if (selectedModVersion == null)
            {
                modMessage = "Please select installed mod, before run game!";
                CreateErrorWindow(mainMessage, modMessage);

                return false;
            }*/

            if (selectedModVersion != null && !selectedModVersion.Installed)
            {
                modMessage = String.Format(LocalizedStrings.Instance["NotInstalled"],
                    selectedModVersion.Name);
                CreateErrorWindow(mainMessage, modMessage);
                return false;
            }

            if (selectedPatchVersion != null && !selectedPatchVersion.Installed)
            {
                modMessage = String.Format(LocalizedStrings.Instance["NotInstalled"],
                    selectedPatchVersion.Name);
                CreateErrorWindow(mainMessage, modMessage);
                return false;
            }

            foreach (var addon in selectedAddonsVersions)
            {
                if (!addon.Installed)
                {
                    modMessage = String.Format(LocalizedStrings.Instance["NotInstalled"], addon.Name);
                    CreateErrorWindow(mainMessage, modMessage);
                    return false;
                }
            }

            return true;
        }

        private bool ModificationsAreReadyToRun()
        {
            string modMessage;

            var mainMessage = LocalizedStrings.Instance["LaunchAborted"];

            var selectedModVersion = DataHandler.GetSelectedModVersion();

            var selectedModData = (ModificationViewModel)ModsList.SelectedItem;

            ModificationViewModel selectedPatchData = null;
            if (PatchesList.SelectedItems.Count > 0)
                selectedPatchData = (ModificationViewModel)PatchesList.SelectedItems[0];

            List<ModificationViewModel> selectedAddonsData = new List<ModificationViewModel>();
            List<ModificationViewModel> selectedGAddonsData = new List<ModificationViewModel>();

            foreach (var data in AddonsList.SelectedItems)
            {
                selectedAddonsData.Add((ModificationViewModel)data);
            }

            /*if (selectedModVersion == null)
            {
                modMessage = "Please select installed mod, before run game!";
                CreateErrorWindow(mainMessage, modMessage);
                return false;
            }*/

            if (selectedModData != null && selectedModData.Downloader != null)
            {
                modMessage = String.Format(LocalizedStrings.Instance["InstallInProgress"], 
                    selectedModVersion.Name);
                CreateErrorWindow(mainMessage, modMessage);
                return false;
            }

            if (selectedPatchData != null && selectedPatchData.Downloader != null)
            {
                modMessage = String.Format(LocalizedStrings.Instance["InstallInProgress"],
                    selectedPatchData.ContainerModification.Name);
                CreateErrorWindow(mainMessage, modMessage);
                return false;
            }

            foreach (var gAddon in selectedGAddonsData)
            {
                if (gAddon.Downloader != null)
                {
                    modMessage = String.Format(LocalizedStrings.Instance["NotInstalled"],
                        gAddon.ContainerModification.Name);
                    CreateErrorWindow(mainMessage, modMessage);
                    return false;
                }
            }

            foreach (var addon in selectedAddonsData)
            {
                if (addon.Downloader != null)
                {
                    modMessage = String.Format(LocalizedStrings.Instance["NotInstalled"],
                        addon.ContainerModification.Name);
                    CreateErrorWindow(mainMessage, modMessage);
                    return false;
                }
            }

            return true;
        }

        private void CreateErrorWindow(string mainMessage, string modMessage)
        {
            var infoWindow = new InfoWindow(mainMessage, modMessage)
                { WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen };
            infoWindow.Continue.Visibility = Visibility.Hidden;
            infoWindow.Cancel.Visibility = Visibility.Hidden;
            infoWindow.ErrorPolygon1.Visibility = Visibility.Visible;
            infoWindow.ErrorPolygon2.Visibility = Visibility.Visible;

            infoWindow.ShowDialog();
        }

        private bool ModificationsDontNeedUpdate()
        {
            var modsMessage = LocalizedStrings.Instance["ModIsUpToDate"];

            var succes = true;

            if (DataHandler.GetSelectedMod() != null)
            {

                var lastVersion = DataHandler.GetSelectedModVersions().OrderBy(m => m).Last();

                if (!lastVersion.Installed)
                {
                    succes = false;
                    modsMessage = String.Format(LocalizedStrings.Instance["UninstalledUpdate"], lastVersion.Name);
                }
            }

            var activePatch = DataHandler.GetSelectedPatch();
            if (activePatch != null)
            {
                var lastPatchVersion = activePatch.ModificationVersions.OrderBy(m => m).LastOrDefault();

                if (lastPatchVersion != null && !lastPatchVersion.Installed)
                {
                    succes = false;
                    modsMessage = String.Format(LocalizedStrings.Instance["UninstalledUpdate"], lastPatchVersion.Name);
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
                        modsMessage = String.Format(LocalizedStrings.Instance["UninstalledUpdate"],
                            selectedAddonLastVersion.Name);
                        break;
                    }
                }
            }

            if (!succes)
            {
                var mainMessage = LocalizedStrings.Instance["ModificationsWithUpdate"];

                var infoWindow = new InfoWindow(mainMessage, modsMessage)
                    { WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen };
                infoWindow.Ok.Visibility = Visibility.Hidden;

                infoWindow.WarningPolygon1.Visibility = Visibility.Visible;
                infoWindow.WarningPolygon2.Visibility = Visibility.Visible;
                infoWindow.WarningPolygon3.Visibility = Visibility.Visible;

                infoWindow.ShowDialog();
                return infoWindow.GetResult();
            }

            return succes;
        }

        private bool ModificationsAreNotDeprecated()
        {
            var modsMessage = LocalizedStrings.Instance["ModIsUpToDate"];

            var succes = true;

            var activePatch = DataHandler.GetSelectedPatch();
            if (activePatch != null && activePatch.Deprecated)
            {
                succes = false;
                modsMessage = String.Format(LocalizedStrings.Instance["Deprecated"], activePatch.Name);
            }

            var selectedAddons = DataHandler.GetSelectedAddonsForSelectedMod()?.Where(m => m != null);

            if (selectedAddons.Count() > 0)
            {
                foreach (var selectedAddon in selectedAddons)
                {
                    if (selectedAddon != null && selectedAddon.Deprecated)
                    {
                        succes = false;
                        modsMessage = String.Format(LocalizedStrings.Instance["Deprecated"], selectedAddon.Name);
                        break;
                    }
                }
            }

            if (!succes)
            {
                var mainMessage = LocalizedStrings.Instance["Compatibility"];

                var infoWindow = new InfoWindow(mainMessage, modsMessage)
                    { WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen };
                infoWindow.Ok.Visibility = Visibility.Hidden;

                infoWindow.WarningPolygon1.Visibility = Visibility.Visible;
                infoWindow.WarningPolygon2.Visibility = Visibility.Visible;
                infoWindow.WarningPolygon3.Visibility = Visibility.Visible;

                infoWindow.ShowDialog();
                return infoWindow.GetResult();
            }

            return succes;
        }

        #endregion

        #region ExeLaunchHandlers

        private async void ButtonWorldBuilder_Click(object sender, RoutedEventArgs e)
        {
            if (DataHandler.GetSelectedMod() != null &&
                DataHandler.GetSelectedMod().ModificationType == ModificationType.Advertising)
                return;

            if (_isWBRunning)
            {
                CreateErrorWindow(LocalizedStrings.Instance["LaunchAborted"], LocalizedStrings.Instance["WorldBuilderRunning"]);
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

            if (ModificationsDontNeedUpdate() && ModificationsAreNotDeprecated())
            {
                if (EntryPoint.SessionInfo.Connected)
                {
                    LauncherUpdate.IsEnabled = false;
                    DisableUI();
                    await CheckAndDowloadWB();

                    SetSelfUpdatingInfo(EntryPoint.SessionInfo.Connected);
                    UpdateProgress.Text = String.Empty;
                    UpdateProgressBar.Value = 0;
                }

                _isWBRunning = true;

                var activeVersions = GetSelectedVersionsOfAllSelectedModifications();
                var doCheck = DoCheck(activeVersions);
                var gameCanBeStarted = await Task.Run(() => GameLauncher.PrepareGame(activeVersions, doCheck));
                EnableUI();

                if (gameCanBeStarted)
                {
                    if (DataHandler.GetHideLauncher())
                        this.Hide();
                    await GameLauncher.RunWB();

                    if (DataHandler.GetHideLauncher())
                        this.Show();
                }
                else
                {
                    ModIncorrectInstallationNotify();
                    GameLauncher.RenameGameFilesToOriginalState();
                }

                _isWBRunning = false;
            }

            SetFocuses();
        }

        private async void Launch_Click(object sender, RoutedEventArgs e)
        {
            if (DataHandler.GetSelectedMod() != null &&
                DataHandler.GetSelectedMod().ModificationType == ModificationType.Advertising)
                return;

            if (_isGameRunning)
            {
                CreateErrorWindow(LocalizedStrings.Instance["LaunchAborted"], LocalizedStrings.Instance["GameRunning"]);
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

            if (NeedToApplyDefaultOptions())
            {
                ApplyDefaultOptions();
            }

            if (ModificationsDontNeedUpdate() && ModificationsAreNotDeprecated())
            {
                DisableUI();
                await CheckModdedExe();
                var activeVersions = GetSelectedVersionsOfAllSelectedModifications();
                _isGameRunning = true;

                var doCheck = DoCheck(activeVersions);

                var gameCanBeStarted = await Task.Run(() => GameLauncher.PrepareGame(activeVersions, doCheck));
                EnableUI();

                if (gameCanBeStarted)
                {
                    if (DataHandler.GetHideLauncher())
                        this.Hide();

                    await CheckAndUpdateGentool();
                    await CheckAndUpdateVulkan();

                    DataHandler.FirstRun = false;
                    var result = await GameLauncher.RunGame();

                    if (DataHandler.GetHideLauncher())
                        this.Show();

                    if (result && ModsList.SelectedItems.Count > 0)
                    {
                        var modContainer = ModsList.SelectedItems[0] as ModificationViewModel;
                        modContainer._GridControls._SupportButton.IsBlinking = true;
                    }
                }
                else
                {
                    ModIncorrectInstallationNotify();
                    GameLauncher.RenameGameFilesToOriginalState();
                }

                _isGameRunning = false;
            }

            SetFocuses();
        }

        #endregion

        #region MainButtonsEvents

        private void UpdateButton_MouseEnter(object sender, MouseEventArgs e)
        {
            _DragAndDropDisable = true;
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

        private async void PatchesButton_Click(object sender, RoutedEventArgs e)
        {
            HideAllLists();
            if (DataHandler.GetSelectedMod() == null)
            {
                DisableUI();
                await DataHandler.ReadOriginalGameAddonsAndPatches();
                EnableUI();
            }
            PatchesList.Visibility = Visibility.Visible;
            ManualAddPatch.Visibility = Visibility.Visible;
        }

        private async void AddonsButton_Click(object sender, RoutedEventArgs e)
        {
            HideAllLists();
            if (DataHandler.GetSelectedMod() == null)
            {
                DisableUI();
                await DataHandler.ReadOriginalGameAddonsAndPatches();
                EnableUI();
            }
            AddonsList.Visibility = Visibility.Visible;
            ManualAddAddon.Visibility = Visibility.Visible;
        }

        private bool DoCheck(List<ModificationVersion> versions)
        {
            if (!DataHandler.GetCheckModFiles() || DataHandler.GetSelectedMod() == null)
                return false;

            var modVersion = versions.Where(m => m.ModificationType == ModificationType.Mod).FirstOrDefault();

            if (!EntryPoint.SessionInfo.Connected || string.IsNullOrEmpty(modVersion.S3HostLink) || string.IsNullOrEmpty(modVersion.S3BucketName))
                return false;

            if (!DataHandler.GetAskBeforeCheck())
                return true;

            var infoWindow = new InfoWindow(LocalizedStrings.Instance["CheckIntegrety"], LocalizedStrings.Instance["TakeLongTime"])
            { WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen };
            infoWindow.ModsMessage.FontSize = 17;
            infoWindow.Ok.Visibility = Visibility.Hidden;
            infoWindow.Continue.Content = LocalizedStrings.Instance["CheckFiles"];
            infoWindow.Cancel.Content = LocalizedStrings.Instance["No"];
            infoWindow.WarningPolygon1.Visibility = Visibility.Visible;
            infoWindow.WarningPolygon2.Visibility = Visibility.Visible;
            infoWindow.WarningPolygon3.Visibility = Visibility.Visible;

            infoWindow.ShowDialog();
            return infoWindow.GetResult();
        }

        private void LauncherUpdate_Click(object sender, RoutedEventArgs e)
        {
            if (_updating)
            {
                LauncherUpdate.Content = LocalizedStrings.Instance["Update"];
                UpdateProgress.Text = String.Empty;
                UpdateProgressBar.Value = 0;
                tokenSource.Cancel();
                EnableUI();
                _updating = false;
            }
            else
            {
                if (downloadingCount != 0)
                    UpdateProgress.Text = LocalizedStrings.Instance["FinishInstall"];
                else
                {
                    UpdateLauncher();
                    LauncherUpdate.Content = LocalizedStrings.Instance["Cancel"];
                    DisableUI();
                    _updating = true;
                }
            }
        }

        private void ButtonOptions_Click(object sender, RoutedEventArgs e)
        {
            if (!_isGameRunning)
            {
                this.Hide();
                var optionsWindow = new OptionsWindow()
                { WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen };
                optionsWindow.ShowDialog();
                this.Show();
                UpdateWindowedStatus();
                DataHandler.SaveLauncherData();
            }
            else
            {
                CreateErrorWindow(LocalizedStrings.Instance["GameIsStillRunning"], LocalizedStrings.Instance["FinishProccess"]);
            }
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
                ButtonWindowed.Content = LocalizedStrings.Instance["ChangeToFullScreen"];
            else
                ButtonWindowed.Content = LocalizedStrings.Instance["ChangeToWindowed"];
        }

        private void ButtonQuickStart_Click(object sender, RoutedEventArgs e)
        {
            DataHandler.SetQuickStartStatus(!DataHandler.IsQuickStart());
            UpdateQuickStartStatus();
        }

        private void UpdateQuickStartStatus()
        {
            if (DataHandler.IsQuickStart())
                ButtonQuickStart.Content = LocalizedStrings.Instance["ChangeToNormalStart"];
            else
                ButtonQuickStart.Content = LocalizedStrings.Instance["ChangeToQuickStart"];
        }

        private void AddMod_Click(object sender, RoutedEventArgs e)
        {
            var addedModificationsNames = DataHandler.GetMods();

            var reposMods = new List<string>();

            if (DataHandler.ReposModsNames != null)
                reposMods = DataHandler.ReposModsNames;

            var notAddedModificationsNames = reposMods
                .Where(t => !addedModificationsNames.Select(m => m.Name.ToLower()).Contains(t.ToLower())).ToList();

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
            var modData = (ModificationViewModel)modGrid.DataContext;


            if (modData.ContainerModification.ModificationType == ModificationType.Advertising &&
                !string.IsNullOrEmpty(modData.ContainerModification.SimpleDownloadLink))
            {
                var newsUrl = modData.ContainerModification.SimpleDownloadLink;
                newsUrl = newsUrl.Replace("&", "^&");
                Process.Start(new ProcessStartInfo("cmd", $"/c start {newsUrl}") { CreateNoWindow = true });
                modData._GridControls._InfoTextBlock.Text = LocalizedStrings.Instance["ThankYou"];
                return;
            }

            if (modData.Downloader == null)
            {
                if ((modData.ContainerModification.Deprecated &&
                     CreateInfoWindowForDeprecatedMod(String.Format(LocalizedStrings.Instance["Deprecated"],
                         modData.ContainerModification.Name))) || !modData.ContainerModification.Deprecated)
                {
                    if (modData.ContainerModification.ModificationType == ModificationType.Mod)
                        ModsList.SelectedItems.Add(modData);

                    if (modData.ContainerModification.ModificationType == ModificationType.Addon)
                        AddonsList.SelectedItems.Add(modData);

                    if (modData.ContainerModification.ModificationType == ModificationType.Patch)
                        PatchesList.SelectedItems.Add(modData);

                    modData._GridControls._UpdateButton.IsEnabled = false;
                    DownloadMod(modData);
                    e.Handled = false;
                }
            }
            else
            {
                modData.CancelDownload();
            }
        }

        private static bool CreateInfoWindowForDeprecatedMod(string modsMessage)
        {
            var mainMessage = LocalizedStrings.Instance["Compatibility"];

            var infoWindow = new InfoWindow(mainMessage, modsMessage)
                { WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen };
            infoWindow.Ok.Visibility = Visibility.Hidden;

            infoWindow.WarningPolygon1.Visibility = Visibility.Visible;
            infoWindow.WarningPolygon2.Visibility = Visibility.Visible;
            infoWindow.WarningPolygon3.Visibility = Visibility.Visible;

            infoWindow.ShowDialog();
            return infoWindow.GetResult();
        }


        static void DownloadProgressChanged(long? totalDownloadSize, long totalBytesRead, double? progressPercentage,
            ModificationViewModel modData, string fileName)
        {
            if (progressPercentage.HasValue)
            {
                var percentage = Convert.ToInt32(progressPercentage.Value);

                string message;

                if (String.IsNullOrEmpty(fileName))
                    message = String.Format(LocalizedStrings.Instance["DownloadInProgress"], (totalBytesRead / 1048576).ToString(),
                        (totalDownloadSize.Value / 1048576).ToString());
                else
                    message = String.Format(LocalizedStrings.Instance["Download"], fileName,
                        (totalBytesRead / 1048576).ToString(), (totalDownloadSize.Value / 1048576).ToString());

                if (percentage == 100)
                {
                    message = LocalizedStrings.Instance["UnpackingPreparing"];
                }

                modData.SetUIMessages(message, percentage);
            }
        }

        private void ModificationDownloadDone(ModificationViewModel modData, DownloadResult result)
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

        private void SuccesModDownload(ModificationViewModel modData)
        {
            if (DataHandler.GetAutoDeleteOldVersionsOption())
            {
                DeleteOutDatedModifications(modData);
            }

            DataHandler.UpdateLocalModificationsData();
            modData.ContainerModification.Installed = true;

            modData.ClearDownloader();
            modData.UpdateUIelements();
            modData.SetUnactiveProgressBar();
        }

        private void DeleteOutDatedModifications(ModificationViewModel modData)
        {
            var versions = modData.ContainerModification.ModificationVersions;

            foreach (var versionData in versions)
                if (versionData != modData.LatestVersion)
                    DataHandler.DeleteVersion(versionData);
        }

        private void DownloadCanceled(ModificationViewModel modData)
        {
            modData.UpdateUIelements();
            modData.SetUIMessages(LocalizedStrings.Instance["Canceled"]);
            modData.SetUnactiveProgressBar();
        }

        private void DownloadCrashed(ModificationViewModel modData, string message)
        {
            modData.UpdateUIelements();
            modData.SetUIMessages(LocalizedStrings.Instance["Error"] + message);
            modData.SetUnactiveProgressBar();
        }

        private void Change_Click(object sender, RoutedEventArgs e)
        {
            var modGrid = (Grid)(((Button)sender).Parent);
            var modData = (ModificationViewModel)modGrid.DataContext;

            var newsUrl = modData.ContainerModification.NewsLink;

            if (modData.ContainerModification.ModificationType == ModificationType.Advertising)
            {
                modData._GridControls._InfoTextBlock.Text = LocalizedStrings.Instance["ThankYou"];
            }

            if (!string.IsNullOrEmpty(newsUrl))
            {
                newsUrl = newsUrl.Replace("&", "^&");
                Process.Start(new ProcessStartInfo("cmd", $"/c start {newsUrl}") { CreateNoWindow = true });
            }
        }

        private void NetworkInfo_Click(object sender, RoutedEventArgs e)
        {
            var modGrid = (Grid)(((Button)sender).Parent);
            var modData = (ModificationViewModel)modGrid.DataContext;

            var networkUrl = modData.ContainerModification.NetworkInfo;

            if (modData.ContainerModification.ModificationType == ModificationType.Advertising)
            {
                modData._GridControls._InfoTextBlock.Text = LocalizedStrings.Instance["ThankYou"];
            }

            if (!string.IsNullOrEmpty(networkUrl))
            {
                networkUrl = networkUrl.Replace("&", "^&");
                Process.Start(new ProcessStartInfo("cmd", $"/c start {networkUrl}") { CreateNoWindow = true });
            }
        }

        private void Support_Click(object sender, RoutedEventArgs e)
        {
            var modGrid = (Grid)(((Button)sender).Parent);
            var modData = (ModificationViewModel)modGrid.DataContext;

            var supportkUrl = modData.ContainerModification.SupportLink;

            modData._GridControls._InfoTextBlock.Text = LocalizedStrings.Instance["ThankYou"];


            if (!string.IsNullOrEmpty(supportkUrl))
            {
                supportkUrl = supportkUrl.Replace("&", "^&");
                Process.Start(new ProcessStartInfo("cmd", $"/c start {supportkUrl}") { CreateNoWindow = true });
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
                var setNameWindow = new ManualAddMidificationWindow(dlg.FileNames.ToList())
                    { WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen };
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

                var name = "";

                if (activeMod == null)
                {
                    name = "Original Game";
                } else
                {
                    name = activeMod.Name;
                }

                var setNameWindow = new ManualAddMidificationWindow(dlg.FileNames.ToList(), name
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

                var name = "";

                if (activeMod == null)
                {
                    name = "Original Game";
                }
                else
                {
                    name = activeMod.Name;
                }

                var setNameWindow = new ManualAddMidificationWindow(dlg.FileNames.ToList(), name
                ) { WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen };
                setNameWindow.CreateAddonCallback += CreateAddonFromFiles;
                setNameWindow.ShowDialog();
            }
        }

        public async void CreateModificationFromFiles(List<string> files, string modName, string version)
        {
            DisableUI();
            await Task.Run(() =>
                ModificationsFileHandler.ExtractModificationFromFiles(files,
                    EntryPoint.GenLauncherModsFolder + '/' + modName + '/' + version));

            DataHandler.UpdateLocalModificationsData();
            var savedModification = DataHandler.GetMods()
                .Where(m => String.Equals(m.Name, modName, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();

            if (ModsListSource.Select(m => m.ContainerModification.Name.ToLower()).Contains(modName.ToLower()))
            {
                var savedMod = ModsListSource.Where(m => String.Equals(m.ContainerModification.Name, modName, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                ModsListSource.Remove(savedMod);
            }

            var modData = new ModificationViewModel(savedModification);
            ModsListSource.Add(modData);
            MoveModInList(modData, ModsListSource.Count - 1, 0);

            EnableUI();
        }

        public async void CreateAddonFromFiles(List<string> files, string path, string modName, string version)
        {
            DisableUI();
            await Task.Run(() => ModificationsFileHandler.ExtractModificationFromFiles(files,
                EntryPoint.GenLauncherModsFolder + '/' + path + '/' + EntryPoint.AddonsFolderName + '/' + modName +
                '/' + version));

            DataHandler.UpdateLocalModificationsData();
            var savedModification = DataHandler.GetAddonsForSelectedMod()
                .Where(m => String.Equals(m.Name, modName, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();

            if (AddonsListSource.Select(m => m.ContainerModification.Name.ToLower()).Contains(modName.ToLower()))
            {
                var savedMod = AddonsListSource.Where(m => String.Equals(m.ContainerModification.Name, modName, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                AddonsListSource.Remove(savedMod);
            }

            var modData = new ModificationViewModel(savedModification);
            AddonsListSource.Add(modData);
            MoveModificationInList(AddonsListSource, modData, AddonsListSource.Count - 1, 0);

            EnableUI();
        }

        public async void CreatePatchFromFiles(List<string> files, string path, string modName, string version)
        {
            DisableUI();
            await Task.Run(() => ModificationsFileHandler.ExtractModificationFromFiles(files,
                EntryPoint.GenLauncherModsFolder + '/' + path + '/' + EntryPoint.PatchesFolderName + '/' + modName +
                '/' + version));

            DataHandler.UpdateLocalModificationsData();
            var savedModification = DataHandler.GetPatchesForSelectedMod()
                .Where(m => String.Equals(m.Name, modName, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();

            if (PatchesListSource.Select(m => m.ContainerModification.Name.ToLower()).Contains(modName.ToLower()))
            {
                var savedMod = PatchesListSource.Where(m => String.Equals(m.ContainerModification.Name, modName, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                PatchesListSource.Remove(savedMod);
            }

            var modData = new ModificationViewModel(savedModification);
            PatchesListSource.Add(modData);
            MoveModificationInList(PatchesListSource, modData, PatchesListSource.Count - 1, 0);

            EnableUI();
        }

        #endregion

        #region ContextMenu Handlers

        private async void ChangeVersionImage(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.DefaultExt = ".big";
            dlg.Filter = LocalizedStrings.Instance["Image"] + " 500x100 (png, jpg)|*png;*jpg;";

            var result = dlg.ShowDialog();

            if (result == true)
            {
                DisableUI();
                var menuItem = (MenuItem)sender;
                var container = ((ModificationViewModel)menuItem.DataContext);

                var filePath = System.IO.Path.Combine(EntryPoint.LauncherFolder, EntryPoint.LauncherImageSubFolder, container.ContainerModification.Name, container.LatestVersion.Version);
                var folderPath = System.IO.Path.Combine(EntryPoint.LauncherFolder, EntryPoint.LauncherImageSubFolder, container.ContainerModification.Name);
                await Task.Run(() => CopyNewModImages(folderPath, filePath, dlg.FileName));
                container.Refresh();
                EnableUI();
            }
        }

        private void CopyNewModImages(string folderPath, string filePath, string filename)
        {
            if (File.Exists(filePath))
                File.Delete(filePath);

            if (File.Exists(filePath + "BW"))
                File.Delete(filePath + "BW");

            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            File.Copy(filename, filePath);

            var image = BlackWhiteImageGenerator.GenerateBlackWhiteBitMapImageFromPath(filePath);
            if (image != null)
                image.Save(filePath + "BW");
        }

        private void OpenGameFolder(object sender, RoutedEventArgs e)
        {
            Process.Start(Directory.GetCurrentDirectory());
        }

        private void OpenModFolder(object sender, RoutedEventArgs e)
        {
            var menuItem = (MenuItem)sender;
            var k = (ModificationViewModel)menuItem.DataContext;

            var selectedVersion = k.ContainerModification.ModificationVersions.Where(m => m.IsSelected).FirstOrDefault();

            if (selectedVersion != null)
            {
                var path = System.IO.Path.Combine(Directory.GetCurrentDirectory(), selectedVersion.GetFolderName());
                if (Directory.Exists(path) && Directory.EnumerateFiles(path).Count() > 0)
                    Process.Start(path);
                else
                {
                    if (k.ContainerModification.ModificationType == ModificationType.Mod)
                       CreateErrorWindow(LocalizedStrings.Instance["UnIsntalledMod"], LocalizedStrings.Instance["NeedToInstallMod"]);

                    if (k.ContainerModification.ModificationType == ModificationType.Addon)
                        CreateErrorWindow(LocalizedStrings.Instance["UnIsntalledAddon"], LocalizedStrings.Instance["NeedToInstallAddon"]);

                    if (k.ContainerModification.ModificationType == ModificationType.Patch)
                        CreateErrorWindow(LocalizedStrings.Instance["UnIsntalledPatch"], LocalizedStrings.Instance["NeedToInstallPatch"]);

                }
            }
        }

        private void CreatePathErrorWindow(string path)
        {
            var infoWindow = new InfoWindow(LocalizedStrings.Instance["CannotFindPath"], path)
            { WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen };
            infoWindow.Continue.Visibility = Visibility.Hidden;
            infoWindow.Cancel.Visibility = Visibility.Hidden;
            infoWindow.ErrorPolygon1.Visibility = Visibility.Visible;
            infoWindow.ErrorPolygon2.Visibility = Visibility.Visible;
            infoWindow.ModsMessage.FontSize = 12;

            infoWindow.ShowDialog();
        }

        private void OpenReplaysFolder(object sender, RoutedEventArgs e)
        {
            var folderPath = "";

            if (EntryPoint.SessionInfo.GameMode == Game.ZeroHour)
                folderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\Command and Conquer Generals Zero Hour Data\\Replays";
            else
                folderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\Command and Conquer Generals Data\\Replays";

            if (Directory.Exists(folderPath))
                Process.Start(folderPath);
            else
                CreatePathErrorWindow(folderPath);
        }

        private void OpenMapsFolder(object sender, RoutedEventArgs e)
        {
            var folderPath = "";

            if (EntryPoint.SessionInfo.GameMode == Game.ZeroHour)
                folderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\Command and Conquer Generals Zero Hour Data\\Maps";
            else
                folderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\Command and Conquer Generals Data\\Maps";

            if (Directory.Exists(folderPath))
                Process.Start(folderPath);
            else
                CreatePathErrorWindow(folderPath);
        }

        private void OpenModdbLink(object sender, RoutedEventArgs e)
        {
            var menuItem = (MenuItem)sender;
            var k = (ModificationViewModel)menuItem.DataContext;

            var moddbUrl = k.ContainerModification.ModDBLink;

            if (!string.IsNullOrEmpty(moddbUrl))
            {
                moddbUrl = moddbUrl.Replace("&", "^&");
                Process.Start(new ProcessStartInfo("cmd", $"/c start {moddbUrl}") { CreateNoWindow = true });
            }
        }

        private void OpenDiscordLink(object sender, RoutedEventArgs e)
        {
            var menuItem = (MenuItem)sender;
            var k = (ModificationViewModel)menuItem.DataContext;

            var discordUrl = k.ContainerModification.DiscordLink;

            if (!string.IsNullOrEmpty(discordUrl))
            {
                discordUrl = discordUrl.Replace("&", "^&");
                Process.Start(new ProcessStartInfo("cmd", $"/c start {discordUrl}") { CreateNoWindow = true });
            }
        }

        private void ContextMenu_Opened(object sender, RoutedEventArgs e)
        {
            _DragAndDropDisable = true;
            var contextMenu = (ContextMenu)sender;
            var mod = ((ModificationViewModel)contextMenu.DataContext).ContainerModification;

            contextMenu.Closed += ContextMenu_Closed;

            if (String.IsNullOrEmpty(mod.ModDBLink))
                RemoveMenuItemByName(contextMenu, "Visit ModdB page");

            if (String.IsNullOrEmpty(mod.DiscordLink))
                RemoveMenuItemByName(contextMenu, "Join Discord Server");

            if (mod.ModificationType == ModificationType.Advertising)
            {
                RemoveMenuItemByName(contextMenu, "Open mod folder");
                RemoveMenuItemByName(contextMenu, "Set image (500x100)");
            }
        }

        private void ContextMenu_Closed(object sender, RoutedEventArgs e)
        {
            _DragAndDropDisable = false;
        }

        private void RemoveMenuItemByName(ContextMenu menu, string name)
        {
            foreach (var item in menu.Items)
            {
                var menuItem = item as MenuItem;
                if (menuItem != null && String.Equals(menuItem.Header.ToString(), name, StringComparison.OrdinalIgnoreCase))
                {
                    menu.Items.Remove(item);
                    return;
                }
            }
        }

        #endregion
    }
}