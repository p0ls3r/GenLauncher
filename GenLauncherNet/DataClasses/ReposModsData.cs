using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenLauncherNet
{
    public class ReposModsData
    {
        public string LauncherVersion { get; set; }
        public string DownloadLink { get; set; }
        public string VulkanReposData { get; set; }

        public List<ModAddonsAndPatches> modDatas = new List<ModAddonsAndPatches>();

        public List<string> globalAddonsData = new List<string>();

        public List<AdvertisingData> AdvData = new List<AdvertisingData>();
    }

    public class ModAddonsAndPatches
    {
        public string ModName { get; set; }
        public string ModLink { get; set; }
        public List<string> ModPatches { get; set; }

        public List<string> ModAddons { get; set; }

        public ModAddonsAndPatches()
        {
            ModPatches = new List<string>();
            ModAddons = new List<string>();
        }
    }

    public class AdvertisingData
    {
        public string ModName { get; set; }
        public string ModLink { get; set; }
        public List<string> ImagesData { get; set; }

        public AdvertisingData()
        {
            ImagesData = new List<string>();
        }
    }
}
