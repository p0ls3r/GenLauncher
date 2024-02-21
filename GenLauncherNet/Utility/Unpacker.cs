using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;

namespace GenLauncherNet
{
    public static class Unpacker
    {
        public static void ExtractDlls()
        {
            CheckAndExtractDllInSubFolder("x64", "7z.dll");
            CheckAndExtractDllInSubFolder("x86", "7z.dll");
        }

        public static void ExtractLangDlls()
        {
            var langHash = new HashSet<string>() { "ar", "de", "es", "fr", "hr", "pt", "ru", "tr", "uk", "zh" };

            var cultureInfo = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

            if (langHash.Contains(cultureInfo))
            {
                CheckAndExtractDllInSubFolder(cultureInfo, "GenLauncher.resources.dll");
            }

            //fucking .net can't extract resource from folder with "-", so it can't extract dll from "zh-Hant" and in the same time, it doesn't know culture zh, only zh-Hant.
            if (cultureInfo == "zh")
            {
                if (Directory.Exists("zh-Hant"))
                {
                    Directory.Delete("zh-Hant", true);
                }
                Directory.Move("zh", "zh-Hant");
            }
        }

        public static void ExctractImages()
        {
            string fileName = "";

            if (EntryPoint.SessionInfo.GameMode == Game.ZeroHour)
                fileName = "uamZH.jpg";
            else
                fileName = "uamG.jpg";

            var filePath = EntryPoint.LauncherFolder + "\\uam.jpg";

            if (!File.Exists(filePath))
            {
                using (var resource = Assembly.GetExecutingAssembly().GetManifestResourceStream("GenLauncherNet.Images." + fileName))
                {
                    using (var file = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                    {
                        resource?.CopyTo(file);
                    }
                }
            }
        }

        public static void ExctractGentoolOptionsFile()
        {
            var fileName = "d3d8.cfg";

            if (!File.Exists(fileName))
            {
                using (var resource = Assembly.GetExecutingAssembly().GetManifestResourceStream("GenLauncherNet." + fileName))
                {
                    using (var file = new FileStream(fileName, FileMode.Create, FileAccess.Write))
                    {
                        resource?.CopyTo(file);
                    }
                }
            }
        }

        private static void Delete7zDllsIfItsOutDated()
        {
            if (File.Exists("SevenZipExtractor.dll"))
            {
                var fileInfo = FileVersionInfo.GetVersionInfo("SevenZipExtractor.dll");

                if (fileInfo.FileVersion == "1.0.15.0")
                {
                    File.Delete("SevenZipExtractor.dll");
                    File.Delete(Path.Combine("x86", "7z.dll"));
                    File.Delete(Path.Combine("x64", "7z.dll"));
                }
            }
        }

        private static void CheckAndExtractDllInSubFolder(string folder, string filename)
        {
            if (File.Exists(folder + "\\" + filename))
            {
                using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("GenLauncherNet.Dlls." + folder + "." + filename))
                {
                    using (MD5 md5 = MD5.Create())
                    {
                        if (stream != null)
                        {
                            var hash = md5.ComputeHash(stream);
                            var resourceHash = BitConverter.ToString(hash).Replace("-", String.Empty);
                            var currentHash = MD5ChecksumCalculator.ComputeMD5Checksum($"{folder + "/" + filename}");
                            if (resourceHash != currentHash)
                            {
                                File.Delete($"{folder + "/" + filename}");
                            }
                        }
                    }
                }
            }

            if (!File.Exists(folder + "\\" + filename))
            {
                using (var resource = Assembly.GetExecutingAssembly().GetManifestResourceStream("GenLauncherNet.Dlls." + folder + "." + filename))
                {
                    if (resource != null)
                    {
                        if (!Directory.Exists(folder))
                            Directory.CreateDirectory(folder);

                        using (var file = new FileStream(folder + "\\" + filename, FileMode.Create, FileAccess.Write))
                        {
                            resource?.CopyTo(file);
                        }
                    }
                }
            }
        }

        private static void CheckAndExtractDll(string dllName)
        {
            if (!File.Exists(dllName))
            {
                using (var resource = Assembly.GetExecutingAssembly().GetManifestResourceStream("GenLauncherNet.Dlls." + dllName))
                {
                    using (var file = new FileStream(dllName, FileMode.Create, FileAccess.Write))
                    {
                        resource?.CopyTo(file);
                    }
                }
            }
        }
    }
}
