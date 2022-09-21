using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using Microsoft.Win32;

namespace GenLauncherNet.Utility
{
    internal static class Utilities
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
        ///     Opens a specific webpage in the user's default browser based on a given URL.
        /// </summary>
        /// <param name="webpageUrl">URL of the webpage to open.</param>
        internal static void OpenWebpageInBrowser(string webpageUrl)
        {
            try
            {
                Process.Start(webpageUrl);
            }
            catch (Win32Exception noBrowser)
            {
                if (noBrowser.ErrorCode == -2147467259)
                    MessageBox.Show(noBrowser.Message);
            }
            catch (Exception other)
            {
                MessageBox.Show(other.Message);
            }
        }
    }
}