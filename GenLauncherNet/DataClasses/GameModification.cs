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
            }
            else
                ModificationVersions.Add(version);

            if (!this.Installed && version.Installed)
                this.Installed = true;

            this.UnionModifications(version);
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