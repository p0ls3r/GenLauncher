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
    public static class DllsUnpacker
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
