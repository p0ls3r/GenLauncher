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

namespace GenLauncherNet
{
    class EntryPoint
    {
        public const string LauncherFolder = @".GenLauncherFolder/";
        public const string ConfigName = @".GenLauncherFolder/GenLauncherCfg.yaml";
        public const string ModsRepos = @"https://raw.githubusercontent.com/p0ls3r/GenLauncherModsData/master/ReposModificationDataMk3.yaml";
        public const string GenLauncherModsFolder = "GenLauncherModifications";
        public const string LauncherImageSubFolder = "LauncherImages";
        public const string Version = "0.0.7.1 Beta";
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

        private static Mutex _mutex1;

        [DllImport("kernel32.dll")]
        static extern bool CreateSymbolicLink(string lpSymlinkFileName, string lpTargetFileName, SymbolicLink dwFlags);

        enum SymbolicLink
        {
            File = 0,
            Directory = 1
        }

        public static HashSet<string> ZHFiles = new HashSet<string>();

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
                    MessageBox.Show("Please move launcher to the Generals ZH game folder");
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
                    MessageBox.Show("Other GenLauncher process is still active, please wait or finish it.");
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
            FillZHFiles();
            SymbolicLinkHandler.DeleteAllSymbolicLinksInGameFolders();
            RenameReplacedFilesBack(new DirectoryInfo(Directory.GetCurrentDirectory()));
            RenameOriginalFilesBack(new DirectoryInfo(Directory.GetCurrentDirectory()));
            DeleteTempFolders(new DirectoryInfo(Directory.GetCurrentDirectory()));
            DeleteOldGenLauncherFile();
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
            ZHFiles.Add("AudioChineseZH.big".ToLower());
            ZHFiles.Add("Gensec.big".ToLower());
            ZHFiles.Add("AudioEnglishZH.big".ToLower());
            ZHFiles.Add("AudioFrenchZH.big".ToLower());
            ZHFiles.Add("AudioGermanZH.big".ToLower());
            ZHFiles.Add("AudioItalianZH.big".ToLower());
            ZHFiles.Add("AudioKoreanZH.big".ToLower());
            ZHFiles.Add("AudioPolishZH.big".ToLower());
            ZHFiles.Add("AudioSpanishZH.big".ToLower());
            ZHFiles.Add("AudioZH.big".ToLower());
            ZHFiles.Add("BrazilianZH.big".ToLower());
            ZHFiles.Add("ChineseZH.big".ToLower());
            ZHFiles.Add("EnglishZH.big".ToLower());
            ZHFiles.Add("FrenchZH.big".ToLower());
            ZHFiles.Add("GermanZH.big".ToLower());
            ZHFiles.Add("ItalianZH.big".ToLower());
            ZHFiles.Add("KoreanZH.big".ToLower());
            ZHFiles.Add("PolishZH.big".ToLower());
            ZHFiles.Add("SpanishZH.big".ToLower());
            ZHFiles.Add("GensecZH.big".ToLower());
            ZHFiles.Add("INIZH.big".ToLower());
            ZHFiles.Add("MapsZH.big".ToLower());
            ZHFiles.Add("Music.big".ToLower());
            ZHFiles.Add("MusicZH.big".ToLower());
            ZHFiles.Add("PatchZH.big".ToLower());
            ZHFiles.Add("ShadersZH.big".ToLower());
            ZHFiles.Add("SpeechBrazilianZH.big".ToLower());
            ZHFiles.Add("SpeechChineseZH.big".ToLower());
            ZHFiles.Add("SpeechEnglishZH.big".ToLower());
            ZHFiles.Add("SpeechFrenchZH.big".ToLower());
            ZHFiles.Add("SpeechGermanZH.big".ToLower());
            ZHFiles.Add("SpeechItalianZH.big".ToLower());
            ZHFiles.Add("SpeechKoreanZH.big".ToLower());
            ZHFiles.Add("SpeechPolishZH.big".ToLower());
            ZHFiles.Add("SpeechSpanishZH.big".ToLower());
            ZHFiles.Add("SpeechZH.big".ToLower());
            ZHFiles.Add("TerrainZH.big".ToLower());
            ZHFiles.Add("TexturesZH.big".ToLower());
            ZHFiles.Add("W3DEnglishZH.big".ToLower());
            ZHFiles.Add("W3DZH.big".ToLower());
            ZHFiles.Add("WindowZH.big".ToLower());
        }

        private static bool IsLauncherInGameFolder()
        {
            //TODO improve checking
            if (File.Exists("generals.exe") && File.Exists("BINKW32.DLL"))
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
