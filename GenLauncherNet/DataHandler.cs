using System;
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

        private static bool connected;
        private static LauncherData Data;
        public static string Version { private set; get; }
        public static string DownloadLink { private set; get; }
        private static GitHubMainDataReader gitHubMainDataReader;
        private static Dictionary<ModificationReposVersion, ModAddonsAndPatches> MofificationsAndAddons;
        private static string startPath = Directory.GetCurrentDirectory();
        private static HashSet<ModificationReposVersion> downloadedModsInfo = new HashSet<ModificationReposVersion>();
        private static HashSet<ModificationVersion> downloadedReposContent = new HashSet<ModificationVersion>();

        public static StringHashSet TempAddedMods = new StringHashSet();

        private static HttpClient LogoDownloader = new HttpClient();

        public static async Task InitData(bool connectionToGithub)
        {
            connected = connectionToGithub;
            if (connected)
            {
                await ReadMainManifest();
            }

            ReadData();
            UpdateModificationsData();

            if (connected)
            {
                var installedMods = GetAddedToMainWindowModifications();
                ReposModsNames = gitHubMainDataReader.GetReposModsNames();

                MofificationsAndAddons = await gitHubMainDataReader.UpdateDownloadedModsDataFromRepos(installedMods);
                var ReposAddons = await gitHubMainDataReader.ReadGlobalAddons();
                var ReposMods = MofificationsAndAddons.Keys.ToList();

                foreach (var reposMod in ReposMods)
                {
                    await DownloadImageIfItNotExist(reposMod);
                    AddDownloadedModificationData(reposMod);
                }

                foreach (var reposAddon in ReposAddons)
                {
                    AddDownloadedModificationData(reposAddon);
                }

                if (Data.SelectedMod != null)
                {
                    await ReadPatchesAndAddonsForMod(Data.SelectedMod);
                }
            }
        }

        #region SettingsData

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

        internal static ModificationReposVersion GetSelectedMod()
        {
            return Data.SelectedMod;
        }

        internal static StringHashSet GetAddedToMainWindowModifications()
        {
            return Data.AddedModifications;
        }

        internal static ModificationVersion GetSelectedModAndItsVersion()
        {
            return Data.GetSelectedModAndItsVersion();
        }

        internal static ModificationVersion GetSelectedPatchAndItsVersion()
        {
            return Data.GetSelectedPatchAndItsVersionForMod(Data.SelectedMod.Name);
        }

        internal static List<ModificationVersion> GetSelectedAddonsAndItsVersions()
        {
            return Data.GetSelectedAddonsAndItsVersions();
        }

        internal static List<ModificationReposVersion> GetSelectedGlobalAddons()
        {
            return Data.SelectedGlobalAddons.ToList();
        }
        internal static List<ModificationReposVersion> GetSelectedAddons()
        {
            return Data.SelectedAddons.ToList();
        }

        internal static ModificationReposVersion GetSelectedPatch()
        {
            if (Data.SelectedMod != null)
                return Data.GetSelectedPatchAndItsVersionForMod(Data.SelectedMod.Name);

            return null;
        }

        public static List<ModificationVersion> GetFullModsVersionsList()
        {
            return Data.GetFullModsVersionsList();
        }

        public static List<ModificationVersion> GetModVersions(ModificationReposVersion modification)
        {
            return Data.GetModVersions(modification);
        }

        public static List<ModificationVersion> GetAddonVersions(ModificationReposVersion modification)
        {
            return Data.GetAddonVersions(modification);
        }

        internal static List<ModificationVersion> GetPatchVersions(ModificationReposVersion modification)
        {
            return Data.GetPatchVersions(modification);
        }

        public static List<ModificationVersion> GetAddonVersionsForModList(string modName)
        {
            return Data.GetAddonsVersionsByMod(modName);
        }

        public static List<ModificationVersion> GetPatchVersionsForModList(string modName)
        {
            return Data.GetPatchesVersionsByMod(modName);
        }

        #endregion

        internal static async Task<ModificationReposVersion> DownloadModificationDataFromRepos(string name)
        {
            var kvp = await gitHubMainDataReader.DownloadModDataByName(name);
            MofificationsAndAddons.Add(kvp.Key, kvp.Value);
            await DownloadImageIfItNotExist(kvp.Key);
            AddDownloadedModificationData(kvp.Key);
            return kvp.Key;
        }

        private static async Task DownloadImageIfItNotExist(ModificationReposVersion mod)
        {
            if (!String.IsNullOrEmpty(mod.UIImageSourceLink))
            {
                try
                {
                    if (!Directory.Exists(Path.Combine(EntryPoint.LauncherFolder, EntryPoint.LauncherImageSubFolder, mod.Name.Replace(":", ""))))
                        Directory.CreateDirectory(Path.Combine(EntryPoint.LauncherFolder, EntryPoint.LauncherImageSubFolder, mod.Name.Replace(":", "")));

                    if (!File.Exists(Path.Combine(EntryPoint.LauncherFolder, EntryPoint.LauncherImageSubFolder, mod.Name, mod.UIImageSourceLink)))
                        using (var stream = await LogoDownloader.GetStreamAsync(mod.UIImageSourceLink))
                        {
                            using (var fileStream = new FileStream(Path.Combine(EntryPoint.LauncherFolder, EntryPoint.LauncherImageSubFolder, mod.Name, mod.Version), FileMode.CreateNew))
                            {
                                await stream.CopyToAsync(fileStream);
                            }
                        }
                }
                catch
                {

                }
            }
        }

        private static void AddDownloadedModificationData(ModificationReposVersion reposVersion)
        {
            Data.AddOrUpdate(new ModificationVersion(reposVersion));
            downloadedReposContent.Add(new ModificationVersion(reposVersion));
        }

        internal static void AddActiveModification(ModificationReposVersion modification)
        {
            if (modification == null)
            {
                Data.SelectedMod = null;
                return;
            }

            switch (modification.ModificationType)
            {
                case ModificationType.Mod:
                    Data.SelectedMod = modification;
                    break;
                case ModificationType.Patch:
                    var tempPatch = new ModificationReposVersion(modification.Name);
                    tempPatch.ModificationType = modification.ModificationType;
                    tempPatch.DependenceName = modification.DependenceName;

                    if (!Data.SelectedPatches.Contains(tempPatch))
                        Data.SelectedPatches.Add(tempPatch);
                    break;

                case ModificationType.Addon:
                    var tempAddon = new ModificationReposVersion(modification.Name);
                    tempAddon.ModificationType = modification.ModificationType;
                    tempAddon.DependenceName = modification.DependenceName;

                    if (!Data.SelectedAddons.Contains(tempAddon))
                        Data.SelectedAddons.Add(tempAddon);
                    break;
            }
        }

        internal static void RemoveActiveModification(ModificationReposVersion modification)
        {
            Data.RemoveModificationFromActive(modification);
        }

        #region UpdateData

        internal static void DeleteVersion(ComboBoxData versionData)
        {
            var modification = versionData.Modification;
            var version = versionData.VersionName;

            var modificationVersion = new ModificationVersion();
            modificationVersion.Name = modification.Name;
            modificationVersion.Version = version;
            modificationVersion.ModificationType = modification.ModificationType;
            modificationVersion.DependenceName = modification.DependenceName;

            DeleteVersion(modificationVersion);            
            UpdateModificationsData();
        }

        internal static void DeleteVersion(ModificationVersion version)
        {
            DeleteModificationVersion(version);
            RemoveActiveModification(version);
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

        public static void UpdateModificationsData()
        {
            AddUnregistredModifications();
            DeleteOutdatedModifications();
        }

        public static void AddAddedModification(string name)
        {
            if (!Data.AddedModifications.Contains(name))
                Data.AddedModifications.Add(name);
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

                    if (!Data.AddedModifications.Contains(modVersion.Name))
                        Data.AddedModifications.Add(modVersion.Name);
                }
            }
        }

        public static void RemoveModificationFromAdded(ModificationReposVersion modification)
        {
            if (modification.ModificationType == ModificationType.Mod)
                Data.RemoveModificationFromAdded(modification.Name);
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
            var modVersions = Data.GetFullModsVersionsList();

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
                var addonsVersions = Data.GetAddonsVersionsByMod(modVersion.Name);
                foreach (var addonVersion in addonsVersions)
                {
                    CheckAddonExistence(addonVersion, EntryPoint.AddonsFolderName);
                }

                //Checking existing in data patches for mod
                var patchesVersions = Data.GetPatchesVersionsByMod(modVersion.Name);
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

        private static async Task ReadMainManifest()
        {
            var reposData = new ReposModsData();

            using (var client = new GitHubCoreYamlReader(EntryPoint.ModsRepos))
            {
                reposData = await client.ReadCoreManifestYaml();
            }

            Version = reposData.LauncherVersion;
            DownloadLink = reposData.DownloadLink;

            gitHubMainDataReader = new GitHubMainDataReader(reposData);
        }

        #endregion

        #region Save/Load
        public static void ReadData()
        {
            try
            {
                CreateLauncherFolder();

                if (!File.Exists(EntryPoint.ConfigName))
                    CreateNewOptionsFile();

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

        private static void CreateNewOptionsFile()
        {
            var data = CreateNewData();            
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

        internal static void UpdateSelectedVersionForModification(ComboBoxData comboBoxData)
        {
            Data.UpdateSelectedVersionForModification(comboBoxData);
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
