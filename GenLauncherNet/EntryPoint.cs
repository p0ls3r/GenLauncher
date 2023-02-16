using GenLauncherNet.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

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
        public const string Version = "1.0.0.1 Release";
        public const int LaunchersCountForUpdateAdverising = 25;

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

        private const uint RequiredNetFrameworkVersionReleaseKey = 528040; // Version 4.8
        private const string RequiredNetFrameworkVersion = "4.8"; // Release key = 528040

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

        [STAThreadAttribute]
        public static void Main()
        {
            try
            {
                var app = new App();

                if (!GeneralUtilities.IsRequiredNetFrameworkVersionInstalled(RequiredNetFrameworkVersionReleaseKey))
                {
                    var result =
                        MessageBox.Show(
                            $".NET Framework {RequiredNetFrameworkVersion} or later is required for GenLauncher. " +
                            "Would you like to download a compatible version?",
                            $".NET Framework {RequiredNetFrameworkVersion} or later required",
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
                    MessageBox.Show("Please move launcher to the Generals ZH or Generals game folder");
                    return;
                }

                CheckDbgCrash();

                if (!CanCreateSymbLink())
                {
                    MessageBox.Show(
                        "Failed to create a test symbolic link. Without the ability to create symbolic links, GenLauncher will not work. \r\rMost often, the inability to create a symbolic link is related to the type of file system where GenLauncher is installed, symbolic links are supported on the NTFS file system, please make sure that GenLauncher is installed on drive with this particular file system. \r\rAlso make sure that the creation of symbolic links does not interfere with the lack of any rights and the work of the anti-virus.");
                    return;
                }

                if (!OtherInstancesExists())
                {
                    MoveExistingWindowOnTop();
                    return;
                }

                PrepareLauncher();

                var initWindow = new InitWindow
                    { WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen };

                app.Run(initWindow);

                ReturnGameFolderToOriginalState();
            }
            catch (Exception e)
            {
                MessageBox.Show(String.Format(
                    "GenLauncher version: {2} \r\n GenLauncher tech support in discord: {3} \r\n Error: {0} \r\n StackTrace: {1} ",
                    e.Message, e.StackTrace, Version, @"https://discord.gg/fFGpudz5hV"));
            }
        }

        private static void PrepareLauncher()
        {
            Unpacker.ExtractDlls();

            GameLauncher.RenameGameFilesToOriginalState();

            DeleteTempFolders(new DirectoryInfo(Directory.GetCurrentDirectory()));
            DeleteOldGenLauncherFile();

            SetSessionInfo();
            SetColorsInfo();

            CheckForCustomVisualInfo();
            CheckForCustomBG();
            ReplaceDlls();

            if (!Directory.Exists(Path.Combine(EntryPoint.LauncherFolder, LauncherImageSubFolder)))
                Directory.CreateDirectory(Path.Combine(EntryPoint.LauncherFolder, LauncherImageSubFolder));

            Unpacker.ExctractImages();
        }

        private static void SetColorsInfo()
        {
            if (SessionInfo.GameMode == Game.ZeroHour)
            {
                DefaultColors = new ColorsInfo("#00e3ff", "DarkGray", "#7a7db0", "#baff0c", "#232977", "#090502",
                    "#B3000000", "White", "#090502", "#F21d2057", "#F21d2057", "#2534ff");
                DefaultColors.GenLauncherBackgroundImage = new ImageBrush(
                    new BitmapImage(new Uri("pack://application:,,,/GenLauncher;component/Images/Background.png")));
            }
            else
            {
                DefaultColors = new ColorsInfo("#ffbb00", "DarkGray", "#ffbb00", "#ffbb00", "#e24c17", "#090502",
                    "#B3000000", "White", "#090502", "#5a210d", "#8a2e0d", "#e24c17");
                DefaultColors.GenLauncherBackgroundImage = new ImageBrush(
                    new BitmapImage(
                        new Uri("pack://application:,,,/GenLauncher;component/Images/BackgroundGenerals.png")));
            }

            Colors = DefaultColors;
        }

        private static void CheckForCustomBG()
        {
            if (!File.Exists("GlBg.png"))
                return;

            DefaultColors.GenLauncherBackgroundImage = new ImageBrush(new BitmapImage(
                new Uri(System.IO.Path.Combine(Directory.GetCurrentDirectory(), @"GlBg.png"), UriKind.Absolute)));
        }

        private static void CheckForCustomVisualInfo()
        {
            if (!File.Exists("Colors.yaml"))
                return;

            var deSerializer = new YamlDotNet.Serialization.Deserializer();

            ColorsInfoString colors = new ColorsInfoString();

            using (FileStream fstream = new FileStream("Colors.yaml", FileMode.OpenOrCreate))
            {
                colors = deSerializer.Deserialize<ColorsInfoString>(new StreamReader(fstream));
            }

            if (colors != null)
                EntryPoint.DefaultColors = new ColorsInfo(colors);
        }

        private static void SetSessionInfo()
        {
            SessionInfo = new SessionInformation();

            if (File.Exists("WindowZH.big"))
            {
                SessionInfo.GameMode = Game.ZeroHour;
                ModsRepos =
                    @"https://raw.githubusercontent.com/p0ls3r/GenLauncherModsData/master/ReposModificationDataZH2.yaml";
                FillZHFiles();
                return;
            }

            if (File.Exists("Window.big"))
            {
                SessionInfo.GameMode = Game.Generals;
                ModsRepos =
                    @"https://raw.githubusercontent.com/p0ls3r/GenLauncherModsData/master/ReposModificationDataGenerals2.yaml";
                FillGeneralsFiles();
                return;
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

        private static void DeleteOldGenLauncherFile()
        {
            var ExutetableFileName = System.IO.Path.GetFileName(Process.GetCurrentProcess().MainModule.FileName);
            var tempFile = ExutetableFileName + "Old";

            try
            {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
            catch
            {
                //TODO logger
            }
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

        private static void FillZHFiles()
        {
            GameFiles.Add("AudioChineseZH.big".ToLower());
            GameFiles.Add("Gensec.big".ToLower());
            GameFiles.Add("AudioEnglishZH.big".ToLower());
            GameFiles.Add("AudioFrenchZH.big".ToLower());
            GameFiles.Add("AudioGermanZH.big".ToLower());
            GameFiles.Add("AudioItalianZH.big".ToLower());
            GameFiles.Add("AudioKoreanZH.big".ToLower());
            GameFiles.Add("AudioPolishZH.big".ToLower());
            GameFiles.Add("AudioSpanishZH.big".ToLower());
            GameFiles.Add("AudioZH.big".ToLower());
            GameFiles.Add("BrazilianZH.big".ToLower());
            GameFiles.Add("ChineseZH.big".ToLower());
            GameFiles.Add("EnglishZH.big".ToLower());
            GameFiles.Add("FrenchZH.big".ToLower());
            GameFiles.Add("GermanZH.big".ToLower());
            GameFiles.Add("ItalianZH.big".ToLower());
            GameFiles.Add("KoreanZH.big".ToLower());
            GameFiles.Add("PolishZH.big".ToLower());
            GameFiles.Add("SpanishZH.big".ToLower());
            GameFiles.Add("GensecZH.big".ToLower());
            GameFiles.Add("INIZH.big".ToLower());
            GameFiles.Add("MapsZH.big".ToLower());
            GameFiles.Add("Music.big".ToLower());
            GameFiles.Add("MusicZH.big".ToLower());
            GameFiles.Add("PatchZH.big".ToLower());
            GameFiles.Add("ShadersZH.big".ToLower());
            GameFiles.Add("SpeechBrazilianZH.big".ToLower());
            GameFiles.Add("SpeechChineseZH.big".ToLower());
            GameFiles.Add("SpeechEnglishZH.big".ToLower());
            GameFiles.Add("SpeechFrenchZH.big".ToLower());
            GameFiles.Add("SpeechGermanZH.big".ToLower());
            GameFiles.Add("SpeechItalianZH.big".ToLower());
            GameFiles.Add("SpeechKoreanZH.big".ToLower());
            GameFiles.Add("SpeechPolishZH.big".ToLower());
            GameFiles.Add("SpeechSpanishZH.big".ToLower());
            GameFiles.Add("SpeechZH.big".ToLower());
            GameFiles.Add("TerrainZH.big".ToLower());
            GameFiles.Add("TexturesZH.big".ToLower());
            GameFiles.Add("W3DEnglishZH.big".ToLower());
            GameFiles.Add("W3DGermanZH.big".ToLower());
            GameFiles.Add("W3DChineseZH.big".ToLower());
            GameFiles.Add("W3DGerman2ZH.big".ToLower());
            GameFiles.Add("W3DItalianZH.big".ToLower());
            GameFiles.Add("W3DKoreanZH.big".ToLower());
            GameFiles.Add("W3DPolishZH.big".ToLower());
            GameFiles.Add("W3DSpanishZH.big".ToLower());
            GameFiles.Add("W3DZH.big".ToLower());
            GameFiles.Add("WindowZH.big".ToLower());
        }

        private static void FillGeneralsFiles()
        {
            GameFiles.Add("Audio.big".ToLower());
            GameFiles.Add("AudioBrazilian.big".ToLower());
            GameFiles.Add("AudioChinese.big".ToLower());
            GameFiles.Add("AudioEnglish.big".ToLower());
            GameFiles.Add("AudioFrench.big".ToLower());
            GameFiles.Add("AudioGerman.big".ToLower());
            GameFiles.Add("AudioGerman2.big".ToLower());
            GameFiles.Add("AudioItalian.big".ToLower());
            GameFiles.Add("AudioKorean.big".ToLower());
            GameFiles.Add("AudioPolish.big".ToLower());
            GameFiles.Add("AudioSpanish.big".ToLower());
            GameFiles.Add("Brazilian.big".ToLower());
            GameFiles.Add("Chinese.big".ToLower());
            GameFiles.Add("English.big".ToLower());
            GameFiles.Add("French.big".ToLower());
            GameFiles.Add("German.big".ToLower());
            GameFiles.Add("German2.big".ToLower());
            GameFiles.Add("Italian.big".ToLower());
            GameFiles.Add("Korean.big".ToLower());
            GameFiles.Add("Polish.big".ToLower());
            GameFiles.Add("Spanish.big".ToLower());
            GameFiles.Add("gensec.big".ToLower());
            GameFiles.Add("INI.big".ToLower());
            GameFiles.Add("maps.big".ToLower());
            GameFiles.Add("Music.big".ToLower());
            GameFiles.Add("Patch.big".ToLower());
            GameFiles.Add("shaders.big".ToLower());
            GameFiles.Add("Speech.big".ToLower());
            GameFiles.Add("SpeechBrazilian.big".ToLower());
            GameFiles.Add("SpeechChinese.big".ToLower());
            GameFiles.Add("SpeechEnglish.big".ToLower());
            GameFiles.Add("SpeechFrench.big".ToLower());
            GameFiles.Add("SpeechGerman.big".ToLower());
            GameFiles.Add("SpeechGerman2.big".ToLower());
            GameFiles.Add("SpeechItalian.big".ToLower());
            GameFiles.Add("SpeechKorean.big".ToLower());
            GameFiles.Add("SpeechPolish.big".ToLower());
            GameFiles.Add("SpeechSpanish.big".ToLower());
            GameFiles.Add("W3DChinese.big".ToLower());
            GameFiles.Add("W3DGerman2.big".ToLower());
            GameFiles.Add("W3DItalian.big".ToLower());
            GameFiles.Add("W3DKorean.big".ToLower());
            GameFiles.Add("W3DPolish.big".ToLower());
            GameFiles.Add("W3DSpanish.big".ToLower());
            GameFiles.Add("Terrain.big".ToLower());
            GameFiles.Add("Textures.big".ToLower());
            GameFiles.Add("W3D.big".ToLower());
            GameFiles.Add("Window.big".ToLower());
        }

        private static bool IsLauncherInGameFolder()
        {
            //TODO improve checking
            if (File.Exists("generals.exe") && File.Exists("BINKW32.DLL") &&
                (File.Exists("WindowZH.big") || File.Exists("Window.big") ||
                 File.Exists("WindowZH.big" + GenLauncherReplaceSuffix) ||
                 File.Exists("Window.big" + GenLauncherReplaceSuffix)))
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