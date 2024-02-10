﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet;

namespace GenLauncherNet
{
    //Class responsible to work with launcher Data
    class DataHandler
    {
        //TODO regions

        public static List<string> ReposModsNames { get; private set; }
        public static VulkanData VulkanData { get; private set; }

        private static bool connected;
        private static LauncherData Data;
        public static string Version { private set; get; }
        public static string DownloadLink { private set; get; }
        private static GitHubMainDataReader gitHubMainDataReader;
        private static Dictionary<ModificationReposVersion, ModAddonsAndPatches> MofificationsAndAddons;
        private static string startPath = Directory.GetCurrentDirectory();
        private static HashSet<ModificationReposVersion> downloadedModsInfo = new HashSet<ModificationReposVersion>();
        private static HashSet<ModificationVersion> downloadedReposContent = new HashSet<ModificationVersion>();
        private static List<AdvertisingData> AdvData = new List<AdvertisingData>();

        private static List<string> OGPatches = new List<string>();
        private static List<string> OGAddons = new List<string>();

        private static ModificationReposVersion Advertising;

        private static HttpClient LogoDownloader = new HttpClient();

        public static async Task InitData(bool connectionToGithub)
        {
            connected = connectionToGithub;
            if (connected)
            {
                await ReadMainManifest();
            }

            ReadLocalModsData();
            UpdateLocalModificationsData();

            if (connected)
            {
                var installedMods = GetMods().Select(t => t.Name.ToLower()).ToList();
                ReposModsNames = gitHubMainDataReader.GetReposModsNames();

                MofificationsAndAddons = await gitHubMainDataReader.UpdateDownloadedModsDataFromRepos(installedMods);
                var ReposMods = MofificationsAndAddons.Keys.ToList();

                foreach (var reposMod in ReposMods)
                {
                    await DownloadImages(reposMod);
                    AddDownloadedModificationData(reposMod);
                }

                //await ReadOriginalGameAddonsAndPatches();

                if (GetSelectedMod() != null)
                {
                    await ReadPatchesAndAddonsForMod(GetSelectedMod());
                }
            }

            if (GetMods().Count > 0)
                FirstRun = false;
        }

        public static async Task ReadOriginalGameAddonsAndPatches()
        {
            if (connected)
            {
                var keyModification = new ModificationReposVersion("Original Game");

                if (downloadedModsInfo.Contains(keyModification))
                    return;
                else
                    downloadedModsInfo.Add(keyModification);

                var reposPatches = await gitHubMainDataReader.ReadAddonsForMod(OGPatches);
                var reposAddons = await gitHubMainDataReader.ReadAddonsForMod(OGAddons);

                foreach (var patch in reposPatches)
                {
                    var add = new ModificationVersion(patch);
                    add.DependenceName = "Original Game";
                    Data.AddOrUpdate(add);
                    downloadedReposContent.Add(new ModificationVersion(patch));
                }

                foreach (var addon in reposAddons)
                {
                    var add = new ModificationVersion(addon);
                    add.DependenceName = "Original Game";
                    Data.AddOrUpdate(add);
                    downloadedReposContent.Add(new ModificationVersion(addon));
                }
            }
        }

        private static async Task ReadMainManifest()
        {
            var reposData = new ReposModsData();

            using (var client = new GitHubYamlReader(EntryPoint.ModsRepos))
            {
                reposData = await client.ReadYaml<ReposModsData>();
            }

            Version = reposData.LauncherVersion;
            DownloadLink = reposData.DownloadLink;

            AdvData = reposData.AdvData;

            gitHubMainDataReader = new GitHubMainDataReader(reposData);

            if (AdvData.Count > 0)
            {
                await DownloadAdvertisingData(gitHubMainDataReader);
            }

            if (!String.IsNullOrEmpty(reposData.VulkanReposData))
            {
                await DownloadVulkanData(reposData.VulkanReposData);
            }

            if (reposData.originalGamePatches.Count > 0)
            {
                OGPatches = reposData.originalGamePatches;
            }

            if (reposData.originalGameAddons.Count > 0)
            {
                OGAddons = reposData.originalGameAddons;
            }
        }

        #region SettingsData

        internal static void SetAskBeforeCheck(bool ask)
        {
            Data.AskBeforeCheck = ask;
        }

        internal static void SetCheckModFiles(bool check)
        {
            Data.CheckModFiles = check;
        }

        internal static void SetHideLauncher(bool hide)
        {
            Data.HideLauncherAfterGameStart = hide;
        }

        internal static void SetLaunchesCount(int count)
        {
            Data.LaunchesCount = count;
        }

        public static void SetGameParams(string param)
        {
            Data.GameParams = param;
        }

        public static string GetGameParams()
        {
            return Data.GameParams;
        }

        public static void SetWindowedStatus(bool status)
        {
            Data.Windowed = status;
        }

        public static void SetCameraHeight(int height)
        {
            Data.CameraHeight = height;
        }

        public static int GetCameraHeight()
        {
            return Data.CameraHeight;
        }

        public static void SetQuickStartStatus(bool status)
        {
            Data.QuickStart = status;
        }

        public static void SetModdedExeStatus(bool status)
        {
            Data.ModdedExe = status;
        }        

        public static bool IsWindowed()
        {
            return Data.Windowed;
        }

        public static bool IsQuickStart()
        {
            return Data.QuickStart;
        }

        public static bool IsModdedExe()
        {
            return Data.ModdedExe;
        }

        public static bool GentoolAutoUpdate()
        {
            return Data.AutoUpdateGentool;
        }

        public static void SetGentoolAutoUpdateStatus(bool status)
        {
            Data.AutoUpdateGentool = status;
        }

        public static void SetAutoDeleteOldVersionsOption(bool status)
        {
            Data.AutoDeleteOldVersions = status;
        }

        public static bool GetAutoDeleteOldVersionsOption()
        {
            return Data.AutoDeleteOldVersions;
        }
        #endregion

        #region DataGetters

        public static bool FirstRun
        {
            get { return Data.FirstStart; }
            set { Data.FirstStart = value; }
        }

        public static bool UseVulkan
        {
            get { return Data.UseVulkan; }
            set { Data.UseVulkan = value; }
        }

        internal static bool GetAskBeforeCheck()
        {
            return Data.AskBeforeCheck;
        }

        internal static bool GetHideLauncher()
        {
            return Data.HideLauncherAfterGameStart;
        }

        internal static bool GetCheckModFiles()
        {
            return Data.CheckModFiles;
        }

        internal static int GetLauncherCount()
        {
            return Data.LaunchesCount;
        }

        internal static ModificationVersion GetAdvertising()
        {
            //TODO maybe in far future make more than 1 advertising
            if (Advertising != null)
                return new ModificationVersion(Advertising);
            else
                return null;
        }

        internal static GameModification GetSelectedMod()
        {
            return Data.Modifications.Where(m => m.IsSelected).FirstOrDefault();
        }

        internal static ModificationVersion GetSelectedModVersion()
        {
            var selectedMod = GetSelectedMod();
            if (selectedMod != null)
                return selectedMod.ModificationVersions.Where(m => m.IsSelected).FirstOrDefault();
            else
                return null;
        }

        internal static ModificationVersion GetSelectedPatchVersion()
        {
            /*if (GetSelectedMod() != null)
            {*/
                return GetPatchesForSelectedMod()
                    .Where(m => m.IsSelected)
                    .SelectMany(m => m.ModificationVersions)
                    .Where(m => m.IsSelected)
                    .FirstOrDefault();
            //}

            //return null;
        }

        internal static List<string> GetAllModificationsNames()
        {
            return Data.Modifications.Select(m => m.Name).ToList();
        }

        internal static List<GameModification> GetPatchesForSelectedMod()
        {
            var selectedMod = GetSelectedMod();

            if (selectedMod != null)
            {
                return Data.Patches.Where(m => String.Equals(m.DependenceName, selectedMod.Name, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }
            else
                return Data.Patches.Where(m => String.Equals(m.DependenceName, "Original game", StringComparison.OrdinalIgnoreCase))
                    .ToList();
        }

        internal static List<GameModification> GetAddonsForSelectedMod()
        {
            var selectedMod = GetSelectedMod();

            if (selectedMod != null)
                return Data.Addons.Where(m => String.Equals(m.DependenceName, selectedMod.Name, StringComparison.OrdinalIgnoreCase))
                    .Union(Data.Addons.Where(m=> String.Equals(m.DependenceName, GetSelectedPatch()?.Name, StringComparison.OrdinalIgnoreCase)))
                    .ToList();
            else
                return Data.Addons.Where(m => String.Equals(m.DependenceName, "Original game", StringComparison.OrdinalIgnoreCase))
                    .Union(Data.Addons.Where(m => String.Equals(m.DependenceName, GetSelectedPatch()?.Name, StringComparison.OrdinalIgnoreCase)))
                    .ToList();
        }

        public static List<ModificationVersion> GetSelectedModVersions()
        {
            return GetSelectedMod().ModificationVersions;
        }

        internal static List<ModificationVersion> GetSelectedAddonsVersions()
        {
            var result = new List<ModificationVersion>();
            var selectedMod = GetSelectedMod();
            /*if (selectedMod != null)
            {*/
                return GetAddonsForSelectedMod()
                    .Where(m => m.IsSelected)
                    .SelectMany(m => m.ModificationVersions.Where(t => t.IsSelected))
                    .Union(Data.Addons.Where(m => String.Equals(m.DependenceName, GetSelectedPatch()?.Name, StringComparison.OrdinalIgnoreCase))
                      .Where(m => m.IsSelected)
                      .SelectMany(m => m.ModificationVersions.Where(t => t.IsSelected)))
                    .ToList();
            /*}

            return result;*/
        }

        internal static List<GameModification> GetSelectedAddonsForSelectedMod()
        {
            return GetAddonsForSelectedMod().Where(m => m.IsSelected).ToList();
        }

        internal static GameModification GetSelectedPatch()
        {
            return GetPatchesForSelectedMod().Where(m => m.IsSelected).FirstOrDefault();
        }

        public static List<ModificationVersion> GetAllModsVersionsList()
        {
            return Data.Modifications
               .Select(m => m.ModificationVersions)
               .SelectMany(m => m)
               .ToList();
        }

        public static List<ModificationVersion> GetAddonVersionsForModList(string modName)
        {
            return Data.Addons.Where(m => String.Equals(m.DependenceName, modName, StringComparison.OrdinalIgnoreCase))
                .SelectMany(m => m.ModificationVersions)
                .Union(Data.Addons.Where(m => String.Equals(m.DependenceName, GetSelectedPatchVersion()?.Name, StringComparison.OrdinalIgnoreCase)).SelectMany(m => m.ModificationVersions))
                .ToList();
        }

        public static List<ModificationVersion> GetPatchVersionsForModList(string modName)
        {
            return Data.Patches.Where(m => String.Equals(m.DependenceName, modName, StringComparison.OrdinalIgnoreCase))
               .SelectMany(m => m.ModificationVersions)
               .ToList();
        }

        #endregion

        internal static void AddModModification(ModificationVersion modification)
        {
            Data.AddOrUpdate(modification);
        }

        internal static void UnselectAllModifications()
        {
            foreach (var mod in Data.Modifications)
                mod.IsSelected = false;
        }

        internal static async Task<ModificationVersion> DownloadModificationDataFromRepos(string name)
        {
            var kvp = await gitHubMainDataReader.DownloadModDataByName(name);
            if (!MofificationsAndAddons.ContainsKey(kvp.Key))
                MofificationsAndAddons.Add(kvp.Key, kvp.Value);

            await DownloadImages(kvp.Key);
            AddDownloadedModificationData(kvp.Key);
            return new ModificationVersion(kvp.Key);
        }

        private static void AddDownloadedModificationData(ModificationReposVersion reposVersion)
        {
            Data.AddOrUpdate(new ModificationVersion(reposVersion));
            downloadedReposContent.Add(new ModificationVersion(reposVersion));
        }

        internal static List<GameModification> GetMods()
        {
            return Data.Modifications;
        }

        #region UpdateData

        internal static void DeleteVersion(ComboBoxData versionData)
        {
            var modification = versionData.SelectedVersion;
            var version = versionData.VersionName;

            var modificationVersion = new ModificationVersion();
            modificationVersion.Name = modification.Name;
            modificationVersion.Version = version;
            modificationVersion.ModificationType = modification.ModificationType;
            modificationVersion.DependenceName = modification.DependenceName;

            DeleteVersion(modificationVersion);
            UpdateLocalModificationsData();
        }

        internal static void DeleteVersion(ModificationVersion version)
        {
            DeleteModificationVersion(version);
        }

        internal static void DeleteModificationVersion(ModificationVersion modificationVersion)
        {
            switch (modificationVersion.ModificationType)
            {
                case ModificationType.Mod:
                    Directory.Delete(EntryPoint.GenLauncherModsFolder + '/' + modificationVersion.Name + '/' + modificationVersion.Version, true);
                    break;
                case ModificationType.Addon:
                    if (String.IsNullOrEmpty(modificationVersion.DependenceName))
                        return;
                    else
                        Directory.Delete(EntryPoint.GenLauncherModsFolder + '/' + modificationVersion.DependenceName + '/' + EntryPoint.AddonsFolderName + '/' + modificationVersion.Name + '/' + modificationVersion.Version, true);
                    break;
                case ModificationType.Patch:
                    Directory.Delete(EntryPoint.GenLauncherModsFolder + '/' + modificationVersion.DependenceName + '/' + EntryPoint.PatchesFolderName + '/' + modificationVersion.Name + '/' + modificationVersion.Version, true);
                    break;
                case ModificationType.Advertising:
                    Data.Delete(modificationVersion);
                    break;
            }
        }

        public static async Task ReadPatchesAndAddonsForMod(ModificationReposVersion modification)
        {
            if (connected)
            {
                var keyModification = new ModificationReposVersion(modification.Name);

                if (downloadedModsInfo.Contains(keyModification))
                    return;
                else
                    downloadedModsInfo.Add(keyModification);

                if (MofificationsAndAddons.ContainsKey(keyModification))
                {
                    var modData = MofificationsAndAddons[keyModification];
                    var reposPatches = await gitHubMainDataReader.ReadAddonsForMod(modData.ModPatches);
                    var reposAddons = await gitHubMainDataReader.ReadAddonsForMod(modData.ModAddons);

                    foreach (var patch in reposPatches)
                    {
                        Data.AddOrUpdate(new ModificationVersion(patch));
                        downloadedReposContent.Add(new ModificationVersion(patch));
                    }

                    foreach (var addon in reposAddons)
                    {
                        Data.AddOrUpdate(new ModificationVersion(addon));
                        downloadedReposContent.Add(new ModificationVersion(addon));
                    }
                }
            }
        }

        public static void UpdateLocalModificationsData()
        {
            AddUnregistredModifications();
            DeleteOutdatedModifications();
        }

        private static void AddUnregistredModifications()
        {
            var modDirectoryInfo = new DirectoryInfo(startPath + "//" + EntryPoint.GenLauncherModsFolder);

            foreach (var directory in modDirectoryInfo.GetDirectories())
            {
                CheckSubDirectoryForModificationsVersions(directory);
            }
        }

        private static void CheckSubDirectoryForModificationsVersions(DirectoryInfo directory)
        {
            foreach (var subDirectory in directory.GetDirectories())
            {
                //Adding addons versions for addons
                if (subDirectory.Name == EntryPoint.AddonsFolderName)
                {
                    foreach (var addonsDirectory in subDirectory.GetDirectories())
                        CheckSubDirectoryForAddonsVersions(addonsDirectory, directory.Name, ModificationType.Addon);
                    continue;
                }

                //Adding patches versions for patches
                if (subDirectory.Name == EntryPoint.PatchesFolderName)
                {
                    foreach (var patchesDirectory in subDirectory.GetDirectories())
                        CheckSubDirectoryForAddonsVersions(patchesDirectory, directory.Name, ModificationType.Patch);
                    continue;
                }

                //Adding modification versions
                if (ModFolderContainsFiles(subDirectory) && !subDirectory.Name.Contains(EntryPoint.GenLauncherVersionFolderCopySuffix))
                {
                    var modVersion = new ModificationVersion();
                    modVersion.Name = directory.Name;
                    modVersion.Installed = true;
                    modVersion.Version = subDirectory.Name;
                    modVersion.ModificationType = ModificationType.Mod;
                    Data.AddOrUpdate(modVersion);
                }
            }
        }

        private static void CheckSubDirectoryForAddonsVersions(DirectoryInfo directory, string dependency, ModificationType type)
        {
            foreach (var subDirectory in directory.GetDirectories())
            {
                if (ModFolderContainsFiles(subDirectory) && !subDirectory.Name.Contains(EntryPoint.GenLauncherVersionFolderCopySuffix))
                {
                    var addonVersion = new ModificationVersion();

                    addonVersion.Name = directory.Name;
                    addonVersion.Version = subDirectory.Name;
                    addonVersion.ModificationType = type;
                    addonVersion.Installed = true;
                    addonVersion.DependenceName = dependency;

                    Data.AddOrUpdate(addonVersion);
                }
            }
        }

        private static void DeleteOutdatedModifications()
        {
            var modVersions = GetAllModsVersionsList().Where(m => m.ModificationType != ModificationType.Advertising);

            foreach (var modVersion in modVersions)
            {
                //Checking existing in data mod
                if (!Directory.Exists(EntryPoint.GenLauncherModsFolder + "\\" + modVersion.Name)
                    || !Directory.Exists(EntryPoint.GenLauncherModsFolder + "\\" + modVersion.Name + "\\" + modVersion.Version)
                    || !ModFolderContainsFiles(new DirectoryInfo(EntryPoint.GenLauncherModsFolder + "\\" + modVersion.Name + "\\" + modVersion.Version)))
                {
                    if (downloadedReposContent.Contains(modVersion))
                        modVersion.Installed = false;
                    else
                        Data.Delete(modVersion);
                }

                //Checking existing in data addons for mod
                var addonsVersions = GetAddonVersionsForModList(modVersion.Name);
                foreach (var addonVersion in addonsVersions)
                {
                    CheckAddonExistence(addonVersion, EntryPoint.AddonsFolderName);
                }

                //Checking existing in data patches for mod
                var patchesVersions = GetPatchVersionsForModList(modVersion.Name);
                foreach (var patchVersion in patchesVersions)
                {
                    CheckAddonExistence(patchVersion, EntryPoint.PatchesFolderName);
                }
            }
        }

        private static void CheckAddonExistence(ModificationVersion modificationVersion, string folderName)
        {
            if (!Directory.Exists(EntryPoint.GenLauncherModsFolder + "\\" + modificationVersion.DependenceName)
                        || !Directory.Exists(EntryPoint.GenLauncherModsFolder + "\\" + modificationVersion.DependenceName + "\\" + folderName)
                        || !Directory.Exists(EntryPoint.GenLauncherModsFolder + "\\" + modificationVersion.DependenceName + "\\" + folderName + "\\" + modificationVersion.Name)
                        || !Directory.Exists(EntryPoint.GenLauncherModsFolder + "\\" + modificationVersion.DependenceName + "\\" + folderName + "\\" + modificationVersion.Name + "\\" + modificationVersion.Version)
                        || !ModFolderContainsFiles(new DirectoryInfo(EntryPoint.GenLauncherModsFolder + "\\" + modificationVersion.DependenceName + "\\" + folderName + "\\" + modificationVersion.Name + "\\" + modificationVersion.Version)))
            {
                if (downloadedReposContent.Contains(modificationVersion))
                    modificationVersion.Installed = false;
                else
                    Data.Delete(modificationVersion);
            }
        }

        private static bool ModFolderContainsFiles(DirectoryInfo directoryInfo)
        {
            foreach (var file in directoryInfo.GetFiles())
            {
                return true;
            }

            foreach (var folder in directoryInfo.GetDirectories())
            {
                if (FolderContainsFiles(folder))
                    return true;
            }

            return false;
        }

        private static bool FolderContainsFiles(DirectoryInfo directoryInfo)
        {
            foreach (var file in directoryInfo.GetFiles())
            {
                return true;
            }

            foreach (var folder in directoryInfo.GetDirectories())
            {
                return FolderContainsFiles(folder);
            }

            return false;
        }        

        private static async Task DownloadVulkanData(string vulkanLink)
        {
            try
            {
                using (var client = new GitHubYamlReader(vulkanLink))
                {
                    VulkanData = await client.ReadYaml<VulkanData>();
                }
            } 
            catch
            {

            }
        }

        private static async Task DownloadAdvertisingData(GitHubMainDataReader gitHubMainDataReader)
        {
            //TODO maybe make more than 1 advertasing in far future
            var advData = AdvData[0];
            Advertising = await gitHubMainDataReader.DownloadAdvertisingInfo(advData.ModLink);

            var folderName = advData.ModName.Trim(Path.GetInvalidFileNameChars());

            if (Directory.Exists(folderName))
            {
                var dirInfo = new DirectoryInfo(Path.Combine(EntryPoint.LauncherFolder, EntryPoint.LauncherImageSubFolder, folderName));
                var filesCount = dirInfo.GetFiles().Length;

                if (filesCount != advData.ImagesData.Count)
                    foreach (var image in dirInfo.GetFiles())
                    {
                        try
                        {
                            File.Delete(image.FullName);
                        }
                        catch
                        {
                            //TODO logger
                        }
                    }
            }

            var i = 0;
            foreach (var imageLink in advData.ImagesData)
            {
                await DownloadImageIfItsNotExist(Path.Combine(EntryPoint.LauncherFolder, EntryPoint.LauncherImageSubFolder, advData.ModName.Trim(Path.GetInvalidFileNameChars())), i.ToString(), imageLink);
                i++;
            }
        }

        #endregion

        private static async Task DownloadImageIfItsNotExist(string path, string fileName, string link)
        {
            try
            {
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                if (!File.Exists(Path.Combine(path, fileName)))
                    using (var stream = await LogoDownloader.GetStreamAsync(link))
                    {
                        using (var fileStream = new FileStream(Path.Combine(path, fileName), FileMode.CreateNew))
                        {
                            await stream.CopyToAsync(fileStream);
                        }
                    }
            }
            catch
            {

            }
        }

        private static async Task DownloadImages(ModificationReposVersion mod)
        {
            if (!String.IsNullOrEmpty(mod.UIImageSourceLink))
                await DownloadImageIfItsNotExist(Path.Combine(EntryPoint.LauncherFolder, EntryPoint.LauncherImageSubFolder, mod.Name), mod.Version, mod.UIImageSourceLink);

            if (mod.ColorsInformation != null && !String.IsNullOrEmpty(mod.ColorsInformation.GenLauncherBackgroundImageLink))
                await DownloadImageIfItsNotExist(Path.Combine(EntryPoint.LauncherFolder, EntryPoint.LauncherImageSubFolder, mod.Name), mod.Version + "bg", mod.ColorsInformation.GenLauncherBackgroundImageLink);
        }

        #region Save/Load
        public static void ReadLocalModsData()
        {
            if (!File.Exists(EntryPoint.ConfigName))
            {
                Data = CreateNewData();
                return;
            }

            try
            {
                CreateLauncherFolder();

                var deSerializer = new YamlDotNet.Serialization.Deserializer();

                using (FileStream fstream = new FileStream(EntryPoint.ConfigName, FileMode.OpenOrCreate))
                {
                    Data = deSerializer.Deserialize<LauncherData>(new StreamReader(fstream));
                }

                if (Data == null)
                    Data = CreateNewData();
            }
            catch
            {
                if (File.Exists(EntryPoint.ConfigName))
                    File.Delete(EntryPoint.ConfigName);
                Data = CreateNewData();
            }
        }

        private static LauncherData CreateNewData()
        {
            return new LauncherData();
        }

        public static void CreateLauncherFolder()
        {
            if (!Directory.Exists(EntryPoint.LauncherFolder))
            {
                var path = EntryPoint.LauncherFolder;

                var folder = Directory.CreateDirectory(path);
                folder.Attributes = FileAttributes.Directory | FileAttributes.Hidden;
            }
        }

        static public void SaveLauncherData()
        {
            try
            {
                if (File.Exists(EntryPoint.ConfigName))
                    File.Delete(EntryPoint.ConfigName);

                var serializer = new YamlDotNet.Serialization.Serializer();

                using (TextWriter writer = File.CreateText(EntryPoint.ConfigName))
                {
                    serializer.Serialize(writer, Data);
                }
            }
            catch
            {

            }
        }
        #endregion
    }
}
