using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenLauncherNet
{
    public class ModificationVersion : ModificationReposVersion, IComparable
    {
        public bool IsSelected = false;
        public bool Installed = false;
        public bool Favorite = false;

        public override bool Equals(object obj)
        {
            if (obj.GetType() != this.GetType()) return false;

            ModificationVersion modificationVersion = (ModificationVersion)obj;
            return (String.Equals(this.Name + this.Version, modificationVersion.Name + modificationVersion.Version, StringComparison.CurrentCultureIgnoreCase));
        }

        public override int GetHashCode()
        {
            return (Name.ToUpper() + Version.ToUpper()).GetHashCode();
        }

        public ModificationVersion()
        {

        }

        public void UnionModifications(ModificationVersion otherModificationVersion)
        {
            if (otherModificationVersion.IsSelected || this.IsSelected)
                this.IsSelected = true;

            if (otherModificationVersion.Installed || this.Installed)
                this.Installed = true;

            if (otherModificationVersion.Favorite || this.Favorite)
                this.Favorite = true;

            //if (!String.IsNullOrEmpty(otherModificationVersion.SimpleDownloadLink) && String.IsNullOrEmpty(this.SimpleDownloadLink))
                this.SimpleDownloadLink = otherModificationVersion.SimpleDownloadLink;

            if (otherModificationVersion.ModificationType != ModificationType.Mod && this.ModificationType == ModificationType.Mod)
                this.ModificationType = otherModificationVersion.ModificationType;

            //if (!String.IsNullOrEmpty(otherModificationVersion.UIImageSourceLink) && String.IsNullOrEmpty(this.UIImageSourceLink))
                this.UIImageSourceLink = otherModificationVersion.UIImageSourceLink;

            //if (!String.IsNullOrEmpty(otherModificationVersion.DependenceName) && String.IsNullOrEmpty(this.DependenceName))
                this.DependenceName = otherModificationVersion.DependenceName;

            //if (!String.IsNullOrEmpty(otherModificationVersion.NewsLink) && String.IsNullOrEmpty(this.NewsLink))
                this.NewsLink = otherModificationVersion.NewsLink;

            //if (!String.IsNullOrEmpty(otherModificationVersion.ModDBLink) && String.IsNullOrEmpty(this.ModDBLink))
                this.ModDBLink = otherModificationVersion.ModDBLink;

            //if (!String.IsNullOrEmpty(otherModificationVersion.DiscordLink) && String.IsNullOrEmpty(this.DiscordLink))
                this.DiscordLink = otherModificationVersion.DiscordLink;

            //if (!String.IsNullOrEmpty(otherModificationVersion.NetworkInfo) && String.IsNullOrEmpty(this.NetworkInfo))
                this.NetworkInfo = otherModificationVersion.NetworkInfo;

            //if (!String.IsNullOrEmpty(otherModificationVersion.S3BucketName) && String.IsNullOrEmpty(this.S3BucketName))
                this.S3BucketName = otherModificationVersion.S3BucketName;

            //if (!String.IsNullOrEmpty(otherModificationVersion.S3FolderName) && String.IsNullOrEmpty(this.S3FolderName))
                this.S3FolderName = otherModificationVersion.S3FolderName;

            //if (!String.IsNullOrEmpty(otherModificationVersion.S3HostLink) && String.IsNullOrEmpty(this.S3HostLink))
                this.S3HostLink = otherModificationVersion.S3HostLink;

            //if (!String.IsNullOrEmpty(otherModificationVersion.S3HostPublicKey) && String.IsNullOrEmpty(this.S3HostPublicKey))
                this.S3HostPublicKey = otherModificationVersion.S3HostPublicKey;

            //if (!String.IsNullOrEmpty(otherModificationVersion.S3HostSecretKey) && String.IsNullOrEmpty(this.S3HostSecretKey))
                this.S3HostSecretKey = otherModificationVersion.S3HostSecretKey;
        }

        public int CompareTo(object o)
        {
            ModificationVersion mv = o as ModificationVersion;
            if (mv != null)
            {
                var thisVersionString = new string(this.Version.ToCharArray().Where(n => n >= '0' && n <= '9').ToArray());
                var otherVersionString = new string(mv.Version.ToCharArray().Where(n => n >= '0' && n <= '9').ToArray());

                while (thisVersionString.Length > otherVersionString.Length)
                    otherVersionString += '0';

                while (thisVersionString.Length < otherVersionString.Length)
                    thisVersionString += '0';

                var thisVersion = int.Parse(thisVersionString);
                var otherVersion = int.Parse(otherVersionString);

                return thisVersion.CompareTo(otherVersion);
            }
            else
                throw new Exception("Cannot compare 2 objects");
        }

        public ModificationVersion(ModificationReposVersion modification)
        {
            this.Name = modification.Name;
            this.Version = modification.Version;
            this.ModificationType = modification.ModificationType;
            this.DependenceName = modification.DependenceName;
            this.ModDBLink = modification.ModDBLink;
            this.DiscordLink = modification.DiscordLink;
            this.SimpleDownloadLink = modification.SimpleDownloadLink;
            this.UIImageSourceLink = modification.UIImageSourceLink;
            this.NewsLink = modification.NewsLink;

            this.NetworkInfo = modification.NetworkInfo;

            this.S3HostLink = modification.S3HostLink;
            this.S3BucketName = modification.S3BucketName;
            this.S3FolderName = modification.S3FolderName;
            this.S3HostPublicKey = modification.S3HostPublicKey;
            this.S3HostSecretKey = modification.S3HostSecretKey;
        }

        public string GetFolderName()
        {
            var versionFolder = string.Empty;

            switch (this.ModificationType)
            {
                case ModificationType.Addon:
                    versionFolder = EntryPoint.GenLauncherModsFolder + '/' + this.DependenceName + '/' + EntryPoint.AddonsFolderName + '/' + this.Name + '/' + this.Version;
                    break;
                case ModificationType.Mod:
                    {
                        versionFolder = EntryPoint.GenLauncherModsFolder + '/' + this.Name + '/' + this.Version;
                    }
                    break;
                case ModificationType.Patch:
                    {
                        versionFolder = EntryPoint.GenLauncherModsFolder + '/' + this.DependenceName + '/' + EntryPoint.PatchesFolderName + '/' + this.Name + '/' + this.Version;
                    }
                    break;
            }

            return versionFolder;
        }
    }
}
