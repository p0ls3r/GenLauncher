using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using Microsoft.Win32;

namespace GenLauncherNet.Utility
{
    /// <summary>
    ///     Utility class providing generally all around useful functions for a variety of tasks.
    /// </summary>
    internal static class GeneralUtilities
    {
        /// <summary>
        /// Checks the registry if there is an installed .NET Framework version matching or exceeding a specified version based on it's release keys.
        /// </summary>
        /// <param name="requiredVersionReleaseKeys">An array of numerical release keys of the .NET Framework version to check for.</param>
        /// <returns>True if the required version is installed; otherwise, false.</returns>
        internal static bool IsRequiredNetFrameworkVersionInstalled(IEnumerable<uint> requiredVersionReleaseKeys)
        {
            const string registrySubKey = @"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full\";

            using (var registryKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Default)
                       .OpenSubKey(registrySubKey))
            {
                var currentlyInstalledReleaseKey = registryKey?.GetValue("Release");

                if (currentlyInstalledReleaseKey != null)
                {
                    // Does the installed .NET Framework version match or exceed any of the release keys?
                    if (requiredVersionReleaseKeys.Any(requiredReleaseKey =>
                            (int)currentlyInstalledReleaseKey >= requiredReleaseKey))
                    {
                        return true; // Required version is installed
                    }
                }
            }

            return false; // Required version not installed
        }

        /// <summary>
        /// Downloads and silently Installs a specified .NET Framework runtime on the user's computer from a given download URL.
        /// </summary>
        /// <param name="downloadUrl">The download URL of the .NET Framework runtime version to download and install.</param>
        internal static void DownloadAndInstallNetFrameworkRuntime(string downloadUrl)
        {
            string tempFilePath = Path.ChangeExtension(Path.GetTempFileName(), ".exe");

            /*
             * Download the specified .NET Framework runtime installer and save it as a temporary file on the user's filesystem
             * which is then run silently and deleted when the installer finishes.
             */
            using (var webClient = new WebClient())
            {
                webClient.DownloadFile(downloadUrl, tempFilePath);
            }

            var installerProcess = new Process();
            installerProcess.StartInfo.FileName = tempFilePath;
            installerProcess.StartInfo.Arguments = "/quiet /norestart";
            installerProcess.Start();
            installerProcess.WaitForExit();
            File.Delete(tempFilePath);
        }
    }
}