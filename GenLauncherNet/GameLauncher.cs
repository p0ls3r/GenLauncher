using GenLauncherNet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GenLauncherNet
{
    public static class GameLauncher
    {
        public async static Task PrepareAndRunGame(List<ModificationVersion> versions)
        {
            PrepareGameFiles(versions);

            await Task.Run(() => RunGame());
            DeactivateGameFiles();
        }

        public async static Task PrepareAndLaunchWorldBuilder(List<ModificationVersion> versions)
        {
            PrepareGameFiles(versions);

            await Task.Run(() => RunWorldBuilder());
            DeactivateGameFiles();
        }

        private static void RemoveNoneGameBigs(DirectoryInfo directoryInfo)
        {
            foreach (var file in directoryInfo.GetFiles())
            {
                try
                {
                    if (Path.GetExtension(file.Name).Contains("big") && BigHandler.IsBigArchive(file.FullName) && !EntryPoint.GameFiles.Contains(file.Name.ToLower()))
                    {
                        if (File.Exists(file.FullName + EntryPoint.GenLauncherReplaceSuffix))
                            File.Delete(file.FullName);
                        else
                            File.Move(file.FullName, file.FullName + EntryPoint.GenLauncherReplaceSuffix);
                    }
                }
                catch
                {
                    //TODO logger
                }
            }

            foreach (var directory in directoryInfo.GetDirectories())
                RemoveNoneGameBigs(directory);
        }

        private static void DeactivateGameFiles()
        {
            SymbolicLinkHandler.DeleteAllSymbolicLinksInGameFolders();

            GameFilesHandler.ActivateGameFilesBack();

            EntryPoint.RenameReplacedFilesBack(new DirectoryInfo(Directory.GetCurrentDirectory()));
            EntryPoint.RenameOriginalFilesBack(new DirectoryInfo(Directory.GetCurrentDirectory()));
        }

        //TODO refactoring, remove converting to tempModifications, remove checking in LocalModificationsHandler
        private static void PrepareGameFiles(List<ModificationVersion> versions)
        {
            RemoveNoneGameBigs(new DirectoryInfo(Directory.GetCurrentDirectory()));

            if (EntryPoint.SessionInfo.GameMode == Game.ZeroHour)
                SetCameraHeight(versions);

            GameFilesHandler.DeactivateGameFiles();

            SymbolicLinkHandler.DeleteAllSymbolicLinksInGameFolders();

            var noNullsVersions = versions;

            foreach (var version in noNullsVersions)
            {
                if (version.ModificationType == ModificationType.Mod)
                {
                    SymbolicLinkHandler.CreateMirrorsFromFolder(EntryPoint.GenLauncherModsFolder + "\\" + version.Name + "\\" + version.Version);
                    continue;
                }

                if (version.ModificationType == ModificationType.Patch)
                {
                    SymbolicLinkHandler.CreateMirrorsFromFolder(EntryPoint.GenLauncherModsFolder + "\\" + version.DependenceName + "\\" + EntryPoint.PatchesFolderName + "\\" + version.Name + "\\" + version.Version);
                    continue;
                }

                if (version.ModificationType == ModificationType.Addon)
                {
                    if (string.IsNullOrEmpty(version.DependenceName))
                    {
                        return;
                    }
                    else
                    {
                        SymbolicLinkHandler.CreateMirrorsFromFolder(EntryPoint.GenLauncherModsFolder + "\\" + version.DependenceName + "\\" + EntryPoint.AddonsFolderName + "\\" + version.Name + "\\" + version.Version);
                        continue;
                    }
                }
            }
        }

        private static void SetCameraHeight(List<ModificationVersion> versions)
        {
            var cameraHeight = DataHandler.GetCameraHeight();

            if (cameraHeight == 0)
                return;

            var bigFiles = new List<string>();

            foreach (var version in versions)
            {
                bigFiles.AddRange(GetBigFilesFromFolder(version.GetFolderName()));
            }

            var bigWithCameraHeight = GetFileWithCameraHeight(bigFiles.OrderBy(f => Path.GetFileName(f)));

            if (!File.Exists(bigWithCameraHeight + EntryPoint.GenLauncherOriginalFileSuffix))
                File.Copy(bigWithCameraHeight, bigWithCameraHeight + EntryPoint.GenLauncherOriginalFileSuffix);

            BigHandler.SetCameraHeight(bigWithCameraHeight, cameraHeight);
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

        private static void RunGame()
        {
            Process process;
            if (DataHandler.IsModdedExe() && File.Exists("modded.exe"))
                process = StartExe("modded.exe");
            else
                process = StartExe("generals.exe");

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

        private static void RunWorldBuilder()
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
    }
}
