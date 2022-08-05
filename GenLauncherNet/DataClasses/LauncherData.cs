using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace GenLauncherNet
{
    //Class for keeping LauncherData and its validation
    public class LauncherData
    {
        public bool ModdedExe { get; set; } = true;
        public bool Windowed { get; set; } = true;
        public bool QuickStart { get; set; } = true;
        public int CameraHeight { get; set; }
        public bool AutoUpdateGentool { get; set; } = true;
        public bool AutoDeleteOldVersions { get; set; } = false;
        public string GameParams { get; set; }

        //Added to main page Modifications
        public StringHashSet AddedModifications = new StringHashSet();

        // Name/ModificationVersions
        public StringConcurrentDictionary<SynchronizedCollection<ModificationVersion>> ModsAndVersions = new StringConcurrentDictionary<SynchronizedCollection<ModificationVersion>>();
        public StringConcurrentDictionary<SynchronizedCollection<ModificationVersion>> GlobalAddonsAndVersions = new StringConcurrentDictionary<SynchronizedCollection<ModificationVersion>>();

        // Dependency Name/Addons and Patches versions        
        public StringConcurrentDictionary<SynchronizedCollection<ModificationVersion>> AddonsAndVersions = new StringConcurrentDictionary<SynchronizedCollection<ModificationVersion>>();
        public StringConcurrentDictionary<SynchronizedCollection<ModificationVersion>> PatchesAndVersions = new StringConcurrentDictionary<SynchronizedCollection<ModificationVersion>>();

        //Selected modification:
        public ModificationReposVersion SelectedMod { get; set; }

        //Selected Addons and patches
        public SynchronizedCollection<ModificationReposVersion> SelectedGlobalAddons { get; set; } = new SynchronizedCollection<ModificationReposVersion>();
        public SynchronizedCollection<ModificationReposVersion> SelectedAddons = new SynchronizedCollection<ModificationReposVersion>();
        public SynchronizedCollection<ModificationReposVersion> SelectedPatches = new SynchronizedCollection<ModificationReposVersion>();

        internal List<ModificationVersion> GetFullModsVersionsList()
        {
            var resultList = new List<ModificationVersion>();
            foreach (var modificationsAndVersions in ModsAndVersions.ToList())
                foreach(var modVersion in modificationsAndVersions.Value.ToList())
                resultList.Add(modVersion);

            return resultList;
        }

        internal List<ModificationVersion> GetSelectedAddonsAndItsVersions()
        {
            var result = new List<ModificationVersion>();
            if (SelectedMod != null && SelectedAddons.Count > 0)
            {
                var tempCollection = new SynchronizedCollection<ModificationVersion>();
                AddonsAndVersions.TryGetValue(SelectedMod.Name, out tempCollection);

                if (tempCollection != null)
                {
                    foreach (var addonVersion in tempCollection.Where(m => m.IsSelected).ToList())
                    {
                        var keyAddon = new ModificationReposVersion(addonVersion.Name);

                        if (SelectedAddons.Contains(keyAddon))
                            result.Add(addonVersion);
                    }
                }
            }

            return result;
        }

        internal ModificationVersion GetSelectedPatchAndItsVersionForMod(string ModName)
        {
            if (!String.IsNullOrEmpty(ModName) && SelectedPatches.Count > 0)
            {
                var tempCollection = new SynchronizedCollection<ModificationVersion>();
                PatchesAndVersions.TryGetValue(SelectedMod.Name, out tempCollection);

                if (tempCollection != null)
                {
                    foreach (var patchVersion in tempCollection.Where(m => m.IsSelected).ToList())
                    {
                        var keyPatch = new ModificationReposVersion(patchVersion.Name);

                        if (SelectedPatches.Contains(keyPatch))
                        {
                            return patchVersion;
                        }
                    }
                }
            }

            return null;
        }

        internal ModificationVersion GetSelectedModAndItsVersion()
        {
            if (SelectedMod != null && ModsAndVersions.ContainsKey(SelectedMod.Name))
                return ModsAndVersions[SelectedMod.Name].Where(m => m.IsSelected).FirstOrDefault();
            else
                return null;
        }

        internal List<ModificationVersion> GetModVersions(ModificationReposVersion modification)
        {
            var versions = new SynchronizedCollection<ModificationVersion>();
            ModsAndVersions.TryGetValue(modification.Name, out versions);

            if (versions != null)
                return versions.ToList();
            else
                return new List<ModificationVersion>();
        }

        internal List<ModificationVersion> GetAddonVersions(ModificationReposVersion modification)
        {
            if (!string.IsNullOrEmpty(modification.DependenceName))
            {
                var versions = new SynchronizedCollection<ModificationVersion>();
                AddonsAndVersions.TryGetValue(modification.DependenceName, out versions);

                if (versions != null)
                    return versions.Where(m => String.Equals(m.Name, modification.Name, StringComparison.OrdinalIgnoreCase)).ToList();
                else
                    return new List<ModificationVersion>();
            }
            else
                return new List<ModificationVersion>();
        }

        internal List<ModificationVersion> GetPatchVersions(ModificationReposVersion modification)
        {
            if (!string.IsNullOrEmpty(modification.DependenceName))
            {

                var versions = new SynchronizedCollection<ModificationVersion>();
                PatchesAndVersions.TryGetValue(modification.DependenceName, out versions);

                if (versions != null)
                    return versions.Where(m => String.Equals(m.Name, modification.Name, StringComparison.OrdinalIgnoreCase)).ToList();
                else
                    return new List<ModificationVersion>();
            }
            else
                return new List<ModificationVersion>();
        }

        internal List<ModificationVersion> GetAddonsVersionsByMod(string modificationName)
        {
            var resultList = new List<ModificationVersion>();
            SynchronizedCollection<ModificationVersion> tempCollection;
            if (AddonsAndVersions.TryGetValue(modificationName, out tempCollection))
            {
                resultList = tempCollection.ToList();
            }

            return resultList;
        }

        internal List<ModificationVersion> GetPatchesVersionsByMod(string modificationName)
        {
            var resultList = new List<ModificationVersion>();
            SynchronizedCollection<ModificationVersion> tempCollection;
            if (PatchesAndVersions.TryGetValue(modificationName, out tempCollection))
            {
                resultList = tempCollection.ToList();
            }

            return resultList;
        }

        internal void AddOrUpdate(ModificationVersion modificationVersion)
        {
            switch (modificationVersion.ModificationType)
            {
                case ModificationType.Mod:
                    AddOrUpdateModification(ModsAndVersions, modificationVersion);
                    break;              
                case ModificationType.Addon:
                    if (String.IsNullOrEmpty(modificationVersion.DependenceName))
                        return;
                    else
                        AddOrUpdateModification(AddonsAndVersions, modificationVersion);
                    break;
                case ModificationType.Patch:
                    AddOrUpdateModification(PatchesAndVersions, modificationVersion);
                    break;
            }
        }

        internal void Delete(ModificationVersion modificationVersion)
        {
            switch (modificationVersion.ModificationType)
            {
                case ModificationType.Mod:
                    {
                        DeleteModification(ModsAndVersions, modificationVersion);
                        DeleteModification(AddonsAndVersions, modificationVersion);
                        DeleteModification(PatchesAndVersions, modificationVersion);
                    }
                    break;
                case ModificationType.Addon:                 
                        DeleteModification(AddonsAndVersions, modificationVersion);
                    break;
                case ModificationType.Patch:
                    DeleteModification(PatchesAndVersions, modificationVersion);
                    break;
            }
        }

        private void DeleteModification(StringConcurrentDictionary<SynchronizedCollection<ModificationVersion>> data, ModificationVersion modificationVersion)
        {
            var tempCollection = new SynchronizedCollection<ModificationVersion>();

            ModificationReposVersion keyModification;

            if (String.IsNullOrEmpty(modificationVersion.DependenceName))
                keyModification = new ModificationReposVersion(modificationVersion.Name);
            else
                keyModification = new ModificationReposVersion(modificationVersion.DependenceName);

            if (data.ContainsKey(keyModification.Name))
            {
                if (data.TryGetValue(keyModification.Name, out tempCollection))
                {
                    if (tempCollection.Contains(modificationVersion))
                    {
                        tempCollection.Remove(modificationVersion);
                        if (tempCollection.Count == 0)
                        {
                            data.TryRemove(keyModification.Name, out _);
                            RemoveModificationFromActive(keyModification);
                        }
                    }
                }
            }
        }

        public void RemoveModificationFromActive(ModificationReposVersion modification)
        {
            if (SelectedAddons.Contains(modification))
                SelectedAddons.Remove(modification);

            if (SelectedPatches.Contains(modification))
                SelectedPatches.Remove(modification);

            if (SelectedGlobalAddons.Contains(modification))
                SelectedGlobalAddons.Remove(modification);            

            if (SelectedMod != null && SelectedMod.Equals(modification))
                SelectedMod = null;
        }

        public void RemoveModificationFromAdded(string name)
        {            
            if (AddedModifications.Contains(name))
                AddedModifications.Remove(name);
        }

        private void AddOrUpdateModification(StringConcurrentDictionary<SynchronizedCollection<ModificationVersion>> VersionData, ModificationVersion modificationVersion)
        {
            var tempCollection = new SynchronizedCollection<ModificationVersion>();
            ModificationReposVersion keyModification;

            if (String.IsNullOrEmpty(modificationVersion.DependenceName))
               keyModification = new ModificationReposVersion(modificationVersion.Name);
            else
               keyModification = new ModificationReposVersion(modificationVersion.DependenceName);

            if (!VersionData.ContainsKey(keyModification.Name))
            {
                tempCollection.Add(modificationVersion);
                VersionData.TryAdd(keyModification.Name, tempCollection);                
            }
            else
            {
                if (VersionData.TryGetValue(keyModification.Name, out tempCollection))
                {
                    if (tempCollection.Contains(modificationVersion))
                    {
                        var index = tempCollection.IndexOf(modificationVersion);
                        var modificationVersionOld = tempCollection[index];

                        modificationVersionOld.UnionModifications(modificationVersion);

                        modificationVersionOld.Name = modificationVersion.Name;
                    }
                    else
                        tempCollection.Add(modificationVersion);
                }
            }
        }

        internal void UpdateSelectedVersionForModification(ComboBoxData comboBoxData)
        {
            var keyModification = new ModificationReposVersion(comboBoxData.Modification.Name);
            keyModification.DependenceName = comboBoxData.Modification.DependenceName;

            switch (comboBoxData.Modification.ModificationType)
            {
                case ModificationType.Mod:
                    UpdateSelectedVersionForModification(ModsAndVersions[keyModification.Name], comboBoxData);
                    break;
                case ModificationType.Addon:
                    if (String.IsNullOrEmpty(comboBoxData.Modification.DependenceName))
                        return;
                    else
                        UpdateSelectedVersionForModification(AddonsAndVersions[keyModification.DependenceName], comboBoxData);
                    break;
                case ModificationType.Patch:
                    UpdateSelectedVersionForModification(PatchesAndVersions[keyModification.DependenceName], comboBoxData);
                    break;
            }
        }

        private void UpdateSelectedVersionForModification(SynchronizedCollection<ModificationVersion> modificationVersions, ComboBoxData comboBoxData)
        {
            foreach (var modificationVersion in modificationVersions.Where(m => String.Equals(comboBoxData.Modification.Name, m.Name, StringComparison.OrdinalIgnoreCase)))
            {
                modificationVersion.IsSelected = false;
                if (string.Equals(modificationVersion.Version, comboBoxData.VersionName, StringComparison.OrdinalIgnoreCase))
                {
                    modificationVersion.IsSelected = true;
                }
            }
        }
    }
}
