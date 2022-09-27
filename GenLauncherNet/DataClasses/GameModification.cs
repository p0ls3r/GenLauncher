using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenLauncherNet
{
    public class GameModification : ModificationVersion
    {
        public List<ModificationVersion> ModificationVersions { get; set; } = new List<ModificationVersion>();

        public int NumberInList { get; set; }

        public GameModification()
        {

        }

        public GameModification(ModificationVersion version)
        {
            this.Name = version.Name;
            this.DependenceName = version.DependenceName;
            UpdateModificationData(version);
        }

        public void UpdateModificationData(ModificationVersion version)
        {
            if (ModificationVersions.Contains(version))
            {
                var modificationVersion = ModificationVersions[ModificationVersions.IndexOf(version)];
                modificationVersion.UnionModifications(version);

                if (this.ModificationType == ModificationType.Advertising)
                    UpdateAdvertising(version);
            }
            else
                ModificationVersions.Add(version);

            if (!this.Installed && version.Installed)
                this.Installed = true;

            this.UnionModifications(version);
        }

        private void UpdateAdvertising(ModificationVersion version)
        {
            this.ModDBLink = version.ModDBLink;
            this.NetworkInfo = version.NetworkInfo;
            this.DiscordLink = version.DiscordLink;
            this.SimpleDownloadLink = version.SimpleDownloadLink;
        }

        public override bool Equals(object obj)
        {
            if (obj.GetType() != this.GetType()) return false;

            GameModification modification = (GameModification)obj;
            return (String.Equals(this.Name.ToLowerInvariant(), modification.Name.ToLowerInvariant(), StringComparison.CurrentCultureIgnoreCase));
        }

        public override int GetHashCode()
        {
            return Name.ToLowerInvariant().GetHashCode();
        }
    }
}