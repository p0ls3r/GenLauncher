using System;
using System.Collections.Generic;

namespace GenLauncherNet
{
    //Class for keeping information needed for launcher
    public class LauncherData
    {
        public bool ModdedExe { get; set; } = true;
        public bool Windowed { get; set; } = true;
        public bool QuickStart { get; set; } = true;
        public int CameraHeight { get; set; }
        public int LaunchesCount { get; set; }
        public bool AutoUpdateGentool { get; set; } = true;
        public bool AutoDeleteOldVersions { get; set; } = false;
        public string GameParams { get; set; }
        public bool CheckModFiles { get; set; } = true;

        public bool AskBeforeCheck { get; set; } = true;
        public bool HideLauncherAfterGameStart { get; set; } = false;
        public bool FirstStart = true;
        public bool UseVulkan = false;

        public List<GameModification> Modifications = new List<GameModification>();
        public List<GameModification> Addons = new List<GameModification>();
        public List<GameModification> Patches = new List<GameModification>();

        internal void AddOrUpdate(ModificationVersion modificationVersion)
        {
            switch (modificationVersion.ModificationType)
            {
                case ModificationType.Mod:
                    AddOrUpdateModificationVersion(Modifications, modificationVersion);
                    break;
                case ModificationType.Advertising:
                    AddOrUpdateModificationVersion(Modifications, modificationVersion);
                    break;
                case ModificationType.Addon:
                    if (String.IsNullOrEmpty(modificationVersion.DependenceName))
                        return;
                    else
                        AddOrUpdateModificationVersion(Addons, modificationVersion);
                    break;
                case ModificationType.Patch:
                    AddOrUpdateModificationVersion(Patches, modificationVersion);
                    break;
            }
        }

        internal void Delete(ModificationVersion modificationVersion)
        {
            switch (modificationVersion.ModificationType)
            {
                case ModificationType.Mod:
                    {
                        DeleteModification(Modifications, modificationVersion);
                        DeleteModification(Addons, modificationVersion);
                        DeleteModification(Patches, modificationVersion);
                    }
                    break;
                case ModificationType.Addon:
                    DeleteModification(Addons, modificationVersion);
                    break;
                case ModificationType.Patch:
                    DeleteModification(Patches, modificationVersion);
                    break;
                case ModificationType.Advertising:
                    DeleteModification(Modifications, modificationVersion);
                    break;
            }
        }

        private void AddOrUpdateModificationVersion(List<GameModification> modificationStorage, ModificationVersion modificationVersion)
        {
            var modification = new GameModification(modificationVersion);

            if (modificationStorage.Contains(modification))
            {
                var savedModificationData = modificationStorage[modificationStorage.IndexOf(modification)];
                savedModificationData.UpdateModificationData(modificationVersion);
            }
            else
                modificationStorage.Add(modification);
        }

        private void DeleteModification(List<GameModification> modificationStorage, ModificationVersion modificationVersion)
        {
            var modification = new GameModification(modificationVersion);

            if (modificationStorage.Contains(modification))
            {
                var savedModificationData = modificationStorage[modificationStorage.IndexOf(modification)];

                if (savedModificationData.ModificationVersions.Contains(modificationVersion))
                    savedModificationData.ModificationVersions.Remove(modificationVersion);

                if (savedModificationData.ModificationVersions.Count == 0)
                    modificationStorage.Remove(modification);
            }
            else
                return;
        }

    }
}