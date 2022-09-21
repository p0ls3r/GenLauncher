using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
        public const string GenLauncherModsFolder = "GenLauncherModifications";
        public const string LauncherImageSubFolder = "LauncherImages";
        public const string Version = "0.0.7.4 Pre Release";
        //public const string Version = "0.0.0.1 Test";
        public const string ModdedExeDownloadLink = @"https://raw.githubusercontent.com/p0ls3r/moddedExe/master/modded.exe";
        public const string AddonsFolderName = "Addons";
        public const string PatchesFolderName = "Patches";
        public const string WorldBuilderExeName = "WorldBuilderNT27.exe";
        public const string WorldBuilderDownloadLink = "https://onedrive.live.com/download?cid=64F1D914DDB8A931&resid=64F1D914DDB8A931%21135&authkey=AJ64UqrGWgfwGm8";
        public const string GenLauncherReplaceSuffix = ".GenLauncherReplaced";
        public const string GenLauncherVersionFolderCopySuffix = ".GenLauncherTempCopy";
        public const string GenLauncherOriginalFileSuffix = ".GenLauncherOriginalFile";
        const string subkey = @"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full\";

        public static SessionInformation SessionInfo;
        public static ColorsInfo Colors;
        public static ColorsInfo DefaultColors;

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
                var app = new App();

                if (!IsNet46Installed())
                {
                    var warningWindow = new Net46NotInstalled() { WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen };
                    app.Run(warningWindow);
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
                    MessageBox.Show("Cannot create symbolic link in game folder, please check Security Policy Setting Create symbolic links field");
                    return;
                }

                if (!OtherInstancesExists())
                {
                    MoveExistingWindowOnTop();
                    return;
                }

                PrepareLauncher();

                if (!Directory.Exists(Path.Combine(EntryPoint.LauncherFolder, LauncherImageSubFolder)))
                    Directory.CreateDirectory(Path.Combine(EntryPoint.LauncherFolder, LauncherImageSubFolder));

                var initWindow = new InitWindow() { WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen };
                
                app.Run(initWindow);

                ReturGameFolderToOriginalState();
            }
            catch (Exception e)
            {
                MessageBox.Show(String.Format("Error: {0} \r\n StackTrace: {1} ", e.Message, e.StackTrace));
            }
        }

        private static void PrepareLauncher()
        {
            DllsUnpacker.ExtractDlls();
            SymbolicLinkHandler.DeleteAllSymbolicLinksInGameFolders();
            RenameReplacedFilesBack(new DirectoryInfo(Directory.GetCurrentDirectory()));
            RenameOriginalFilesBack(new DirectoryInfo(Directory.GetCurrentDirectory()));
            DeleteTempFolders(new DirectoryInfo(Directory.GetCurrentDirectory()));
            DeleteOldGenLauncherFile();

            SetSessionInfo();
            SetColorsInfo();

            CheckForVisualInfo();
        }

        private static void CheckForVisualInfo()
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
                Colors = new ColorsInfo(colors);
        }

        private static void SetColorsInfo()
        {
            if (SessionInfo.GameMode == Game.ZeroHour)
            {
                DefaultColors = new ColorsInfo("#00e3ff", "DarkGray", "#7a7db0", "#baff0c", "#232977", "#090502", "#B3000000", "White", "#090502", "#F21d2057", "#F21d2057", "#2534ff");
                DefaultColors.GenLauncherBackgroundImage = new ImageBrush(new BitmapImage(new Uri("pack://application:,,,/GenLauncher;component/Images/Background.png")));
            }
            else
            {
                DefaultColors = new ColorsInfo("#ffbb00", "DarkGray", "#ffbb00", "#ffbb00", "#e24c17", "#090502", "#B3000000", "White", "#090502", "#5a210d", "#8a2e0d", "#e24c17");
                DefaultColors.GenLauncherBackgroundImage = new ImageBrush(new BitmapImage(new Uri("pack://application:,,,/GenLauncher;component/Images/BackgroundGenerals.png")));
            }

            Colors = DefaultColors;
        }

        private static void SetSessionInfo()
        {
            SessionInfo = new SessionInformation();

            if (File.Exists("W3DZH.big"))
            {
                SessionInfo.GameMode = Game.ZeroHour;
                ModsRepos = @"https://raw.githubusercontent.com/p0ls3r/GenLauncherModsData/master/ReposModificationDataMk3.yaml";
                FillZHFiles();
                return;
            }

            if (File.Exists("W3D.big"))
            {
                SessionInfo.GameMode = Game.Generals;
                ModsRepos = @"https://raw.githubusercontent.com/p0ls3r/GenLauncherModsData/master/ReposModificationDataGenerals.yaml";
                FillGeneralsFiles();
                return;
            }
        }

        private static void ReturGameFolderToOriginalState()
        {
            DataHandler.SaveLauncherData();
            SymbolicLinkHandler.DeleteAllSymbolicLinksInGameFolders();
            RenameReplacedFilesBack(new DirectoryInfo(Directory.GetCurrentDirectory()));
            RenameOriginalFilesBack(new DirectoryInfo(Directory.GetCurrentDirectory()));
            DeleteTempFolders(new DirectoryInfo(Directory.GetCurrentDirectory()));
            GameFilesHandler.ActivateGameFilesBack();
        }

        public static bool IsNet46Installed()
        {
            using (var ndpKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32).OpenSubKey(subkey))
            {
                if (ndpKey != null && ndpKey.GetValue("Release") != null)
                {
                    var releaseKey = (int)ndpKey.GetValue("Release");
                    if (releaseKey >= 393295)
                        return true;

                    return false;
                }
                else
                {
                    return false;
                }
            }
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
            GameFiles.Add("Terrain.big".ToLower());
            GameFiles.Add("Textures.big".ToLower());
            GameFiles.Add("W3D.big".ToLower());
            GameFiles.Add("Window.big".ToLower());
        }

        private static bool IsLauncherInGameFolder()
        {
            //TODO improve checking
            if (File.Exists("generals.exe") && File.Exists("BINKW32.DLL") && (File.Exists("W3DZH.big") || File.Exists("W3D.big")))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static void RenameReplacedFilesBack(DirectoryInfo directoryInfo)
        {
            foreach (var file in directoryInfo.GetFiles())
            {
                if (file.FullName.Contains(EntryPoint.GenLauncherReplaceSuffix))
                {
                    if (!File.Exists(file.FullName.Replace(EntryPoint.GenLauncherReplaceSuffix, string.Empty)))
                        File.Move(file.FullName, file.FullName.Replace(EntryPoint.GenLauncherReplaceSuffix, string.Empty));
                    else
                        File.Delete(file.FullName);
                    continue;
                }

                if (file.FullName.Contains(EntryPoint.GenLauncherOriginalFileSuffix))
                {
                    if (!File.Exists(file.FullName.Replace(EntryPoint.GenLauncherReplaceSuffix, string.Empty)))
                        File.Move(file.FullName, file.FullName.Replace(EntryPoint.GenLauncherReplaceSuffix, string.Empty));
                    else
                    {
                        File.Delete(file.FullName.Replace(EntryPoint.GenLauncherReplaceSuffix, string.Empty));
                        File.Move(file.FullName, file.FullName.Replace(EntryPoint.GenLauncherReplaceSuffix, string.Empty));
                    }
                }
            }

            foreach (var dirInfo in directoryInfo.GetDirectories())
            {
                if (!dirInfo.Name.Contains(GenLauncherModsFolder))
                    RenameReplacedFilesBack(dirInfo);
            }
        }

        public static void RenameOriginalFilesBack(DirectoryInfo directoryInfo)
        {
            foreach (var file in directoryInfo.GetFiles())
            {
                try
                {
                    if (file.FullName.Contains(EntryPoint.GenLauncherOriginalFileSuffix))
                    {
                        if (!File.Exists(file.FullName.Replace(EntryPoint.GenLauncherOriginalFileSuffix, string.Empty)))
                            File.Move(file.FullName, file.FullName.Replace(EntryPoint.GenLauncherOriginalFileSuffix, string.Empty));
                        else
                        {
                            File.Delete(file.FullName.Replace(EntryPoint.GenLauncherOriginalFileSuffix, string.Empty));
                            File.Move(file.FullName, file.FullName.Replace(EntryPoint.GenLauncherOriginalFileSuffix, string.Empty));
                        }
                    }
                }
                catch
                {
                    //TODO Logger
                }
            }

            foreach (var dirInfo in directoryInfo.GetDirectories())
            {
                RenameOriginalFilesBack(dirInfo);
            }
        }

        public static void DeleteTempFolders(DirectoryInfo directoryInfo)
        {
            foreach (var dirInfo in directoryInfo.GetDirectories())
            {
                if (dirInfo.Name.Contains(GenLauncherVersionFolderCopySuffix))
                    Directory.Delete(dirInfo.FullName, true);
                else
                {
                    DeleteTempFolders(dirInfo);
                }
            }
        }
    }
}
