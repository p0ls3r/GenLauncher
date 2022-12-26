using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace GenLauncherNet
{
    public static class Unpacker
    {
        public static void ExtractDlls()
        {
            CheckAndExtractDll("SevenZipExtractor.dll");
            CheckAndExtractDll("SymbolicLinkSupport.dll");
            CheckAndExtractDll("YamlDotNet.dll");
            CheckAndExtractDll("Minio.dll");
            CheckAndExtractDll("RestSharp.dll");
            CheckAndExtractDll("System.Reactive.dll");
            CheckAndExtract7zDll("x64");
            CheckAndExtract7zDll("x86");
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

        private static void CheckAndExtract7zDll(string folder)
        {
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            if (!File.Exists(folder + "\\" + "7z.dll"))
            {
                using (var resource = Assembly.GetExecutingAssembly().GetManifestResourceStream("GenLauncherNet.Dlls." + folder + "." + "7z.dll"))
                {
                    using (var file = new FileStream(folder + "\\" + "7z.dll", FileMode.Create, FileAccess.Write))
                    {
                        resource?.CopyTo(file);
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
