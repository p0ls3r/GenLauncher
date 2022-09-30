using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using SymbolicLinkSupport;

namespace GenLauncherNet
{
    public static class SymbolicLinkHandler
    {
        [DllImport("kernel32.dll")]
        static extern bool CreateSymbolicLink(string lpSymlinkFileName, string lpTargetFileName, SymbolicLink dwFlags);

        private static string startPath = Directory.GetCurrentDirectory();

        enum SymbolicLink
        {
            File = 0,
            Directory = 1
        }

        public static void DeleteAllSymbolicLinksInGameFolders()
        {
            DeleteAllSymbolicLinksInGameFolders(new DirectoryInfo(startPath));
        }

        public static void DeleteAllSymbolicLinksInGameFolders(DirectoryInfo directoryInfo)
        {
            foreach (var file in directoryInfo.GetFiles())
            {
                try
                {
                    if (file.Name != "SymbolicLinkSupport.dll" && file.IsSymbolicLink())
                    {
                        File.Delete(file.FullName);
                    }
                }
                catch
                {
                    //TODO logger
                }
            }
            foreach (var dirInfo in directoryInfo.GetDirectories())
                DeleteAllSymbolicLinksInGameFolders(dirInfo);
        }

        public static void CreateMirrorsFromFolder(string path)
        {
            CreateMirrorsFromFolder(path, string.Empty);
        }

        public static void CreateMirrorsFromFolder(string sourceFolder, string targetFolder)
        {
            if (!string.IsNullOrEmpty(targetFolder) && !Directory.Exists(targetFolder))
            {
                Directory.CreateDirectory(targetFolder);
            }

            var modDirectoryInfo = new DirectoryInfo(sourceFolder);

            foreach (var file in modDirectoryInfo.GetFiles().Where(f => !String.Equals(Path.GetExtension(f.FullName), ".exe", StringComparison.OrdinalIgnoreCase))
                .Where(f => !String.Equals(Path.GetExtension(f.FullName), ".dll", StringComparison.OrdinalIgnoreCase)))
            {
                var sourceFile = file.FullName;
                var targetFile = String.Empty;
                if (String.IsNullOrEmpty(targetFolder))
                    targetFile = file.Name;
                else
                    targetFile = targetFolder + "\\" + file.Name;

                if (sourceFile.Contains(EntryPoint.GenLauncherOriginalFileSuffix))
                    continue;

                if (BigHandler.IsBigArchive(sourceFile))
                {
                    try
                    {
                        CreateMirrorForBig(sourceFile, targetFile);
                    }
                    catch (Exception e)
                    {
                        throw new Exception("Cannot replace file " + targetFile + " ErrorMsg: " + e.Message);
                    }
                }
                else
                {
                    try
                    {
                        CreateMirrorForNonBig(sourceFile, targetFile);
                    }
                    catch (Exception e)
                    {
                        throw new Exception("Cannot replace file " + targetFile + " ErrorMsg: " + e.Message);
                    }
                }
            }

            foreach (var folder in modDirectoryInfo.GetDirectories())
            {
                if (string.IsNullOrEmpty(targetFolder))
                    CreateMirrorsFromFolder(folder.FullName, folder.Name);
                else
                    CreateMirrorsFromFolder(folder.FullName, targetFolder + "\\" + folder.Name);
            }
        }

        public static void CreateMirrorForBig(string sourceFile, string targetFile)
        {
            if (File.Exists(Path.ChangeExtension(targetFile, "big")))
            {
                if (File.Exists(Path.ChangeExtension(targetFile, "big") + EntryPoint.GenLauncherReplaceSuffix) || (new FileInfo(Path.ChangeExtension(targetFile, "big"))).IsSymbolicLink())
                    File.Delete(Path.ChangeExtension(targetFile, "big"));
                else
                    File.Move(Path.ChangeExtension(targetFile, "big"), Path.ChangeExtension(targetFile, "big") + EntryPoint.GenLauncherReplaceSuffix);
            }
            CreateSymbolicLink(Path.ChangeExtension(targetFile, "big"), sourceFile, SymbolicLink.File);
        }

        public static void CreateMirrorForNonBig(string sourceFile, string targetFile)
        {
            if (File.Exists(targetFile))
            {
                try
                {
                    if(File.Exists(targetFile + EntryPoint.GenLauncherReplaceSuffix))
                        File.Delete(targetFile + EntryPoint.GenLauncherReplaceSuffix);

                    File.Move(targetFile, targetFile + EntryPoint.GenLauncherReplaceSuffix);
                }
                catch (Exception e)
                {
                    throw new Exception("Cannot replace file " + targetFile + " ErrorMsg: " + e.Message);
                }
            }
            CreateSymbolicLink(targetFile, sourceFile, SymbolicLink.File);
        }
    }
}
