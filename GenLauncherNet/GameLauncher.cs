using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GenLauncherNet
{
    public static class GameLauncher
    {
        private static HashSet<string> customFileExtensions = new HashSet<string> {".w3d", ".dds", ".tga", ".ini", ".scb", ".wnd", ".csf", ".str" };
        private static HashSet<string> extensionsToCheckHash = new HashSet<string> {".w3d", "big", "bik", ".dds", ".tga", ".ini", ".scb", ".wnd", ".csf", ".str", ".bik" };
        public static HashSet<string> exceptExtensions = new HashSet<string> { ".exe", ".dll" };

        public async static Task<bool> PrepareGame(List<ModificationVersion> versions, bool DoCheck)
        {
            if (DoCheck && EntryPoint.SessionInfo.Connected)
            {
                var modVersion = versions.Where(v => v.ModificationType == ModificationType.Mod).ToList();
                if (modVersion.Count > 0)
                {
                    await Task.Run(() => PrepareGameFiles(modVersion, false, true));
                    var r = await ModFilesAreCorrect(modVersion[0]);
                    await Task.Run(() => RenameGameFilesToOriginalState());
                    if (!r)
                        return false;
                }
            }

            await Task.Run(() => PrepareGameFiles(versions, true, false));
            return true;
        }

        public async static Task<bool> RunGame()
        {
            var result = await Task.Run(() => StartGameExe());
            RenameGameFilesToOriginalState();
            return result;
        }

        public async static Task RunWB()
        {
            await Task.Run(() => StartWBExe());
            RenameGameFilesToOriginalState();
        }

        #region Files Checkings

        private static async Task<bool> ModFilesAreCorrect(ModificationVersion version)
        {
            if (version == null)
                return true;

            var modVersion = DataHandler.GetSelectedModVersion();

            //check only if launching latest version
            if (modVersion != version)
                return true;

            if (string.IsNullOrEmpty(modVersion.S3HostLink) || string.IsNullOrEmpty(modVersion.S3BucketName))
                return true;

            try
            {
                var storage = new S3StorageHandler();
                var modFilesList = await storage.GetModInfo(modVersion);

                if (!AreModFilesCorrect(modFilesList))
                {
                    return false;
                }
                else
                    return true;
            }
            catch
            {
                return true;
            }
        }

        private static bool AreModFilesCorrect(List<ModificationFileInfo> filesInfo)
        {
            foreach (var info in filesInfo)
            {
                if (exceptExtensions.Contains(Path.GetExtension(info.FileName).ToLower()))
                    continue;

                var createdFileName = String.Empty;                

                if (File.Exists(info.FileName))
                    createdFileName = info.FileName;

                if (File.Exists(Path.ChangeExtension(info.FileName, "big")))
                    createdFileName = Path.ChangeExtension(info.FileName, "big");

                if (string.IsNullOrEmpty(createdFileName))
                    return false;                

                if ((extensionsToCheckHash.Contains(Path.GetExtension(createdFileName).ToLower()) || BigHandler.IsBigArchive(createdFileName)) &&
                    !String.Equals(MD5ChecksumCalculator.ComputeMD5Checksum(createdFileName), info.Hash, StringComparison.OrdinalIgnoreCase))
                    return false;
            }
            return true;
        }

        #endregion

        #region File Handlers

        public static void RenameGameFilesToOriginalState()
        {
            FilesHandler.ApplyActionsToGameFiles(SymbolicLinkHandler.RemoveSymbLinkFile, RemoveGenLauncherReplaceSuffixes);
        }

        private static void PrepareGameFiles(List<ModificationVersion> versions, bool setCameraHeight, bool createLinksOnEmptyBigs = false)
        {
            FilesHandler.ApplyActionsToGameFiles(RenameNonGameBigFile, RenameCustomFiles, SymbolicLinkHandler.RemoveSymbLinkFile);

            if (DataHandler.GetSelectedMod() == null)
            {
                RemoveFileSuffix(Directory.GetCurrentDirectory() + "/Data/Scripts/MultiplayerScripts.scb.GLR", EntryPoint.GenLauncherReplaceSuffix);
                RemoveFileSuffix(Directory.GetCurrentDirectory() + "/Data/Scripts/Scripts.ini.GLR", EntryPoint.GenLauncherReplaceSuffix);
                RemoveFileSuffix(Directory.GetCurrentDirectory() + "/Data/Scripts/SkirmishScripts.scb.GLR", EntryPoint.GenLauncherReplaceSuffix);
            }

            if (setCameraHeight && EntryPoint.SessionInfo.GameMode == Game.ZeroHour)
                SetCameraHeight(versions);

            versions.ForEach(v => SymbolicLinkHandler.CreateMirrorsFromFolder(v.GetFolderName(), createLinksOnEmptyBigs));
        }

        private static void RemoveGenLauncherReplaceSuffixes(FileInfo file)
        {
            if (file.FullName.Contains(EntryPoint.GenLauncherReplaceSuffix))
            {
                RemoveFileSuffix(file.FullName, EntryPoint.GenLauncherReplaceSuffix);
                return;
            }

            if (file.FullName.Contains(EntryPoint.GenLauncherOriginalFileSuffix))
            {
                RemoveFileSuffix(file.FullName, EntryPoint.GenLauncherOriginalFileSuffix);
            }
        }

        private static void RenameNonGameBigFile(FileInfo file)
        {
            if (Path.GetExtension(file.FullName).Contains("big") && BigHandler.IsBigArchive(file.FullName) && !EntryPoint.GameFiles.Contains(file.Name.ToLower()))
            {
                ReplaceFile(file.FullName);
            }
        }

        private static void RenameCustomFiles(FileInfo file)
        {
            if (customFileExtensions.Contains(Path.GetExtension(file.FullName)) && !file.FullName.Contains(EntryPoint.GenLauncherModsFolder))
            {
                ReplaceFile(file.FullName);
            }
        }

        private static void ReplaceFile(string fileName)
        {
            if (File.Exists(fileName + EntryPoint.GenLauncherReplaceSuffix))
                File.Delete(fileName);
            else
                File.Move(fileName, fileName + EntryPoint.GenLauncherReplaceSuffix);
        }

        private static void RemoveFileSuffix(string fileName, string suffix)
        {
            try
            {
                if (!File.Exists(fileName.Replace(suffix, string.Empty)))
                    File.Move(fileName, fileName.Replace(suffix, string.Empty));
                else
                {
                    File.Delete(fileName.Replace(suffix, string.Empty));
                    File.Move(fileName, fileName.Replace(suffix, string.Empty));
                }
            }
            catch
            {
                //TODO logger
            }
        }

        #endregion

        #region Camera Height

        private static string SetCameraHeight(List<ModificationVersion> versions)
        {
            var cameraHeight = DataHandler.GetCameraHeight();

            if (cameraHeight == 0)
                return string.Empty;

            var bigFiles = new List<string>();

            foreach (var version in versions)
            {
                bigFiles.AddRange(GetBigFilesFromFolder(version.GetFolderName()));
            }

            var bigWithCameraHeight = GetFileWithCameraHeight(bigFiles.OrderBy(f => Path.GetFileName(f)));

            if (String.IsNullOrEmpty(bigWithCameraHeight))
                return bigWithCameraHeight;

            if (!File.Exists(bigWithCameraHeight + EntryPoint.GenLauncherOriginalFileSuffix))
                File.Copy(bigWithCameraHeight, bigWithCameraHeight + EntryPoint.GenLauncherOriginalFileSuffix);

            BigHandler.SetCameraHeight(bigWithCameraHeight, cameraHeight);
            return bigWithCameraHeight;
        }

        private static string GetFileWithCameraHeight(IOrderedEnumerable<string> files)
        {
            return files.Where(f => FileContainsCameraHeight(f)).FirstOrDefault();
        }

        private static bool FileContainsCameraHeight(string file)
        {
            return BigHandler.FileContainsGameDataIni(file);
        }

        private static List<string> GetBigFilesFromFolder(string folder)
        {
            var result = new List<string>();

            foreach (var file in Directory.GetFiles(folder))
            {
                if (BigHandler.IsBigArchive(file))
                    result.Add(file);
            }

            foreach (var dir in Directory.GetDirectories(folder))
            {
                result.AddRange(GetBigFilesFromFolder(dir));
            }

            return result;
        }

        #endregion

        #region Exe handlers

        private static bool StartGameExe()
        {
            Process process;
            if (DataHandler.IsModdedExe() && File.Exists("modded.exe"))
                process = StartExe("modded.exe");
            else
                process = StartExe("generals.exe");

            var exeRunning = true;

            if (DataHandler.UseVulkan)
                process.PriorityClass = ProcessPriorityClass.High;

            process.Exited += (sender, e1) =>
            {
                exeRunning = false;
            };

            var result = false;
            var secondsPassed = 0;

            while (exeRunning)
            {
                Thread.Sleep(5000);

                if (secondsPassed < 120000 && !result)
                    secondsPassed += 5000;
            }

            if (secondsPassed >= 12000)
                result = true;

            return result;
        }

        private static void StartWBExe()
        {
            Process process;
            

            if (File.Exists(EntryPoint.WorldBuilderExeName))
                process = StartExe(EntryPoint.WorldBuilderExeName);
            else
                process = StartExe("WorldBuilder.exe");

            var exeRunning = true;            

            process.Exited += (sender, e1) =>
            {
                exeRunning = false;
            };

            while (exeRunning)
            {
                Thread.Sleep(5000);
            }
        }

        private static Process StartExe(string exeName)
        {
            Process process;
            

            var parameters = "";

            if (DataHandler.IsWindowed())
                parameters += "-win ";

            if (DataHandler.IsQuickStart())
                parameters += " -quickstart -noshellmap ";

            parameters += " " + DataHandler.GetGameParams();

            process = Process.Start(exeName, parameters);
            process.EnableRaisingEvents = true;

            return process;
        }

        #endregion
    }
}
