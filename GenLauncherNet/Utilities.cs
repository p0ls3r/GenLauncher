using Microsoft.Win32;

namespace GenLauncherNet
{
    internal static class Utilities
    {
        /// <summary>
        ///     Checks the registry if there is a installed .Net Framework version matching or exceeding a specified version.
        /// </summary>
        /// <param name="requiredVersionReleaseKey">Numerical release key of the .Net Framework version to check for.</param>
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
    }
}