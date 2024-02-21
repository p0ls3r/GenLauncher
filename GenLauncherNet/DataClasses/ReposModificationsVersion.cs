using System;

namespace GenLauncherNet
{
    //Information about latest version of modification, takes from github manifests
    public class ModificationReposVersion
    {
        public ModificationType ModificationType { get; set; }
        public string Name { get; set; }
        public string Version { get; set; }
        public string SimpleDownloadLink { get; set; }
        public string UIImageSourceLink { get; set; }
        public string DiscordLink { get; set; }
        public string ModDBLink { get; set; }
        public string NewsLink { get; set; }
        public string DependenceName { get; set; }

        public string S3HostLink { get; set; }
        public string S3BucketName { get; set; }
        public string S3FolderName { get; set; }

        public string S3HostPublicKey { get; set; }
        public string S3HostSecretKey { get; set; }

        public string NetworkInfo { get; set; }
        public bool Deprecated { get; set; }
        public string SupportLink { get; set; }

        public ColorsInfoString ColorsInformation { get; set; }

        public ModificationReposVersion()
        {

        }

        public ModificationReposVersion(string name, string version, string downloadLink = null, string imageSource = null)
        {
            Name = name;
            Version = version;
            UIImageSourceLink = imageSource;
            SimpleDownloadLink = downloadLink;
        }

        public ModificationReposVersion(string name)
        {
            Name = name;
        }


        public override bool Equals(object obj)
        {
            if (obj.GetType() != this.GetType() && !(obj is ModificationVersion)) return false;

            ModificationReposVersion modification = (ModificationReposVersion)obj;
            return (String.Equals(this.Name, modification.Name, StringComparison.CurrentCultureIgnoreCase));
        }

        public override int GetHashCode()
        {
            return Name.ToUpper().GetHashCode();
        }

        public override string ToString()
        {
            return Name;
        }
    }

    public enum ModificationType
    {
        Mod,
        Addon,
        Patch,
        Advertising
    }
}