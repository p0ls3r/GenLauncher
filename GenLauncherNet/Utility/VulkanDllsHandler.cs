using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace GenLauncherNet
{
    public static class VulkanDllsHandler
    {
        public static string GetVulkanIntegrationDllVersion()
        {
            var fileName = Path.Combine(EntryPoint.VulkanDllsFolderName, "d3d8x.dll");

            try
            {
                return FileVersionInfo.GetVersionInfo(fileName).FileVersion;
            }
            catch
            {
                return "-1";
            }
        }

        public static bool IsCurrentVersionOutDated()
        {
            if (DataHandler.VulkanData == null)
                return false;

            var currentVersionString =
                new string(GetVulkanIntegrationDllVersion().ToCharArray().Where(n => n >= '0' && n <= '9').ToArray());
            var latestVersionString =
                new string(DataHandler.VulkanData.LatestVersion.ToCharArray().Where(n => n >= '0' && n <= '9').ToArray());

            while (currentVersionString.Length > latestVersionString.Length)
                latestVersionString += '0';

            while (currentVersionString.Length < latestVersionString.Length)
                currentVersionString += '0';

            var currentVersion = int.Parse(currentVersionString);
            var latestVersion = int.Parse(latestVersionString);

            return (latestVersion > currentVersion);
        }

        public static void CreateVulkanSymbLinks()
        {
            foreach (var file in Directory.GetFiles(EntryPoint.VulkanDllsFolderName))
            {
                if (DataHandler.GentoolAutoUpdate())
                {
                    SymbolicLinkHandler.CreateMirrorForNonBig(file, Path.GetFileName(file));
                }
                else
                {
                    if (String.Equals(Path.GetFileName(file), "d3d8x.dll", StringComparison.OrdinalIgnoreCase))
                    {
                        SymbolicLinkHandler.CreateMirrorForNonBig(file, "d3d8.dll");
                    }
                    else
                    {
                        SymbolicLinkHandler.CreateMirrorForNonBig(file, Path.GetFileName(file));
                    }
                }
                
            }
        }
    }
}
