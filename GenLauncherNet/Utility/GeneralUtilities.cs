using System.Diagnostics;
using System.IO;
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
        ///     Checks the registry if there is a installed .NET Framework version matching or exceeding a specified version.
        /// </summary>
        /// <param name="requiredVersionReleaseKey">Numerical release key of the .NET Framework version to check for.</param>
        /// <returns></returns>
        internal static bool IsRequiredNetFrameworkVersionInstalled(uint requiredVersionReleaseKey)
        {
            const string registrySubKey = @"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full\";

            using (var ndpKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32)
                       .OpenSubKey(registrySubKey))
            {
                if (ndpKey?.GetValue("Release") != null)
                    // Does it match or exceed the required version?
                    return (int)ndpKey.GetValue("Release") >=
                           requiredVersionReleaseKey;
            }

            return false; // Not installed
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