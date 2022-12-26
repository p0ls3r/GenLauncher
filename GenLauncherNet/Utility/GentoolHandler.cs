using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace GenLauncherNet
{
    public static class GentoolHandler
    {
        //Some code taked and refactored from ContraLauncher(https://github.com/ContraMod/Launcher), thanks to launcher author - tet.

        [DllImport("version.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int GetFileVersionInfoSize(string lptstrFilename, out int lpdwHandle);
        [DllImport("version.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool GetFileVersionInfo(string lptstrFilename, int dwHandle, int dwLen, byte[] lpData);
        [DllImport("version.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool VerQueryValue(byte[] pBlock, string lpSubBlock, out IntPtr lplpBuffer, out int puLen);

        private static bool connected;
        private static string latestVersion;

        public static async Task<bool> CanConnectToGentoolWebSite()
        {
            connected = await CheckConnection("http://www.gentool.net/");
            return connected;
        }

        public static bool IsGentoolOutDated()
        {
            try
            {
                if (connected)
                {
                    latestVersion = GetGentoolLatestVersion();
                    if (GetCurrentGentoolVersion() < ParseGentoolVersionToInt(latestVersion))
                    {
                        return true;
                    }
                }
            }
            catch
            {
                //TODO logger
                return false;
            }

            return false;
        }

        public static string GetGentoolDownloadLink()
        {
            return String.Format("http://www.gentool.net/download/GenTool_v{0}.zip", latestVersion);
        }

        public static int GetCurrentGentoolVersion()
        {
            try
            {
                var size = GetFileVersionInfoSize("d3d8.dll", out _);
                if (size == 0) { throw new Win32Exception(); };
                var bytes = new byte[size];
                bool success = GetFileVersionInfo("d3d8.dll", 0, size, bytes);
                if (!success) { throw new Win32Exception(); }

                // 040904E4 US English + CP_USASCII
                VerQueryValue(bytes, @"\StringFileInfo\040904E4\ProductVersion", out IntPtr ptr, out _);
                return int.Parse(Marshal.PtrToStringUni(ptr));
            }
            catch
            {
                return -1;
            }
        }

        private static string GetGentoolLatestVersion()
        {
            var httpClient = new HttpClient();
            var response = httpClient.GetAsync("https://www.gentool.net/").GetAwaiter().GetResult();
            string responseBody = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            var strings = responseBody.Split('\n');

            var versionNumber = "";

            foreach (var line in strings)
            {
                if (line.Contains("var gentool_ver"))
                {
                    versionNumber = new string(line.ToCharArray().Where(n => n >= '0' && n <= '9' || n == '.').ToArray());
                    break;
                }
            }
            if (!String.IsNullOrEmpty(versionNumber))
                return versionNumber;
            else
                throw new ArgumentException("Cannot find gentool_ver on https://www.gentool.net/");
        }

        private static int ParseGentoolVersionToInt(string version)
        {
            var versionDigits = version.Split('.');

            var stringVersion = "";

            foreach (var element in versionDigits)
                stringVersion += element;

            return int.Parse(stringVersion);
        }

        public static async Task<bool> CheckConnection(string url)
        {
            HttpClient client = new HttpClient();

            try
            {
                HttpResponseMessage response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                return true;
            }
            catch
            {
                return false;
                //TODO logger
            }
        }

        public static void SetRecommendedWindoweOptions()
        {
            var fileName = "d3d8.cfg";

            if (!File.Exists(fileName))
            {
                Unpacker.ExctractGentoolOptionsFile();
                return;
            }

            var gentoolOptions = new List<string>();

            foreach (var line in File.ReadLines("d3d8.cfg"))
            {
                if (line.Contains("window ="))
                    gentoolOptions.Add("window=3");
                else
                    gentoolOptions.Add(line);
            }

            File.WriteAllLines(fileName, gentoolOptions);
        }
    }
}
