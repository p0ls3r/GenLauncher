using GenLauncherNet.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WPFLocalizeExtension.Engine;

namespace GenLauncherNet
{
    class EntryPoint
    {
        public const string LauncherFolder = @".GenLauncherFolder/";
        public const string ConfigName = @".GenLauncherFolder/GenLauncherCfg.yaml";
        public static string ModsRepos;
        public const string GenLauncherModsFolder = "GLM";
        public const string GenLauncherModsFolderOld = "GenLauncherModifications";
        public const string LauncherImageSubFolder = "LauncherImages";
        public const string Version = "1.0.0.6";
        public const int LaunchesCountForUpdateAdverising = 50;

        public const string ZHRepos =
            @"https://raw.githubusercontent.com/p0ls3r/GenLauncherModsData/master/ReposModificationDataZH3.yaml";

        public const string GenRepos =
           @"https://raw.githubusercontent.com/p0ls3r/GenLauncherModsData/master/ReposModificationDataGenerals3.yaml";

        //public const string Version = "0.0.0.1 Test";
        public const string ModdedExeDownloadLink =
            @"https://raw.githubusercontent.com/p0ls3r/moddedExe/master/modded.exe";

        public const string AddonsFolderName = "Addons";
        public const string PatchesFolderName = "Patches";
        public const string WorldBuilderExeName = "WorldBuilderNT27.exe";
        public const string VulkanDllsFolderName = "Vulkan";

        public const string WorldBuilderDownloadLink =
            "https://onedrive.live.com/download?cid=64F1D914DDB8A931&resid=64F1D914DDB8A931%21135&authkey=AJ64UqrGWgfwGm8";

        public const string GenLauncherReplaceSuffix = ".GLR";
        public const string GenLauncherVersionFolderCopySuffix = ".GLTC";
        public const string GenLauncherOriginalFileSuffix = ".GOF";

        public static SessionInformation SessionInfo;
        public static ColorsInfo Colors;
        public static ColorsInfo DefaultColors;

        private const uint RequiredNetFrameworkVersionReleaseKey = 393295; // Version 4.6
        private const string RequiredNetFrameworkVersion = "4.6"; // Release key = 393295

        private static Mutex _mutex1;

        [DllImport("kernel32.dll")]
        static extern bool CreateSymbolicLink(string lpSymlinkFileName, string lpTargetFileName, SymbolicLink dwFlags);

        [DllImport("user32.dll")]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        static extern bool ShowWindowAsync(HandleRef hWnd, int nCmdShow);

        private const int SW_RESTORE = 9;

        enum SymbolicLink
        {
            File = 0,
            Directory = 1
        }

        public static HashSet<string> GameFiles = new HashSet<string>();

        [System.STAThreadAttribute()]
        public static void Main()
        {
            try
            {
                if (File.Exists(LauncherFolder + "eng"))
                {
                    LocalizeDictionary.Instance.Culture = new System.Globalization.CultureInfo("en-us");
                } else
                {
                    LocalizeDictionary.Instance.Culture = new System.Globalization.CultureInfo(System.Globalization.CultureInfo.InstalledUICulture.Name);
                }

               
                Unpacker.ExtractLangDlls();                

                if (!GeneralUtilities.IsRequiredNetFrameworkVersionInstalled(RequiredNetFrameworkVersionReleaseKey))
                {
                    var result =
                        MessageBox.Show(
                            String.Format(LocalizedStrings.Instance["NetRequired"], RequiredNetFrameworkVersion) +
                            LocalizedStrings.Instance["DownloadNet"],
                            String.Format(LocalizedStrings.Instance["NetRequired2"], RequiredNetFrameworkVersion),
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Warning
                        );

                    if (result == MessageBoxResult.Yes)
                    {
                        GeneralUtilities.OpenWebpageInBrowser(
                            "https://dotnet.microsoft.com/en-us/download/dotnet-framework/thank-you/net48-web-installer");
                    }

                    return;
                }

                if (!IsLauncherInGameFolder())
                {
                    MessageBox.Show(LocalizedStrings.Instance["MoveLauncher"]);
                    return;
                }

                CheckDbgCrash();

                if (!CanCreateSymbLink())
                {
                    MessageBox.Show(
                        LocalizedStrings.Instance["SymbLink"]);
                    return;
                }

                if (!OtherInstancesExists())
                {
                    MoveExistingWindowOnTop();
                    return;
                }                

                var app = new App();

                var initWindow = new InitWindow()
                    { WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen };

                app.Run(initWindow);

                ReturnGameFolderToOriginalState();
            }
            catch (Exception e)
            {
                MessageBox.Show(String.Format(
                    LocalizedStrings.Instance["ErrorMsg"],
                    e.Message, e.StackTrace, Version, @"https://discord.gg/fFGpudz5hV"));
            }
        }   

        private static void ReturnGameFolderToOriginalState()
        {
            DeleteTempFolders(new DirectoryInfo(Directory.GetCurrentDirectory()));
            GameLauncher.RenameGameFilesToOriginalState();
        }

        public static bool OtherInstancesExists()
        {
            bool createdNew;
            _mutex1 = new Mutex(initiallyOwned: true, "GenLauncher", out createdNew);

            if (!createdNew)
            {
                Thread.Sleep(5000);
                _mutex1.Close();

                _mutex1 = new Mutex(initiallyOwned: true, "GenLauncher", out createdNew);

                return createdNew;
            }

            return true;
        }

        private static void MoveExistingWindowOnTop()
        {
            var proc = Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName).FirstOrDefault();
            var windowHandle = proc.MainWindowHandle;
            ShowWindowAsync(new HandleRef(null, windowHandle), SW_RESTORE);
            SetForegroundWindow(windowHandle);
        }        

        public static bool CanCreateSymbLink()
        {
            var pathOriginal = Path.Combine(Directory.GetCurrentDirectory(), "GenLauncherTestFile.test");
            var pathSymb = Path.Combine(Directory.GetCurrentDirectory(), "GenLauncherTestSymbFile.test");

            using (FileStream fs = File.Create(pathOriginal))
            {
            }

            CreateSymbolicLink(pathSymb, pathOriginal, SymbolicLink.File);

            if (File.Exists(pathSymb))
            {
                File.Delete(pathSymb);
                File.Delete(pathOriginal);
                return true;
            }
            else
            {
                File.Delete(pathOriginal);
                return false;
            }
        }

        public static void CheckDbgCrash()
        {
            if (File.Exists("dbghelp.dll"))
                File.Delete("dbghelp.dll");
        }

        public static void ReplaceDlls()
        {
            ReplaceFileIfItExists("d3d8x.dll");
            ReplaceFileIfItExists("d3d9.dll");
            ReplaceFileIfItExists("d3d10core.dll");
            ReplaceFileIfItExists("d3d11.dll");
        }

        private static void ReplaceFileIfItExists(string filename)
        {
            if (File.Exists(filename))
                File.Move(filename, filename + GenLauncherReplaceSuffix);
        }        

        private static bool IsLauncherInGameFolder()
        {
            //TODO improve checking
            if (File.Exists("generals.exe") && File.Exists("BINKW32.DLL") &&
                (File.Exists("WindowZH.big") || File.Exists("Window.big") || File.Exists("WindowZH.big" + GenLauncherReplaceSuffix) || File.Exists("Window.big" + GenLauncherReplaceSuffix)))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static void DeleteTempFolders(DirectoryInfo directoryInfo)
        {
            foreach (var dirInfo in directoryInfo.GetDirectories())
            {
                if (dirInfo.Name.Contains(GenLauncherVersionFolderCopySuffix))
                {
                    RecursiveDeleteFolder(dirInfo);
                }
                else
                    DeleteTempFolders(dirInfo);
            }
        }

        private static void RecursiveDeleteFolder(DirectoryInfo directoryInfo)
        {
            foreach (var dirInfo in directoryInfo.GetDirectories())
                RecursiveDeleteFolder(dirInfo);

            DeleteFilesInFolder(directoryInfo);

            try
            {
                Directory.Delete(directoryInfo.FullName, true);
            }
            catch
            {
                //TODO logger
            }
        }

        private static void DeleteFilesInFolder(DirectoryInfo directoryInfo)
        {
            foreach (var file in directoryInfo.GetFiles())
            {
                try
                {
                    File.Delete(file.FullName);
                }
                catch
                {
                    //TODO logger
                }
            }
        }
    }
}