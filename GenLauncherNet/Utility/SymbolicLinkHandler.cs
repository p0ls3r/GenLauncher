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

        public static void RemoveSymbLinkFile(FileInfo file)
        {
            try
            {
                if (file.IsSymbolicLink() && !file.FullName.Contains("SymbolicLinkSupport.dll"))
                {
                    File.Delete(file.FullName);
                }
            }
            catch
            {
                //TODO logger
            }
        }

        public static void CreateMirrorsFromFolder(string path, bool createLinksOnEmptyBigs, bool exceptExeAndDllsFiles = true)
        {
            CreateMirrorsFromFolder(path, string.Empty, createLinksOnEmptyBigs, exceptExeAndDllsFiles);
        }

        public static void CreateMirrorsFromFolder(string sourceFolder, string targetFolder, bool createLinksOnEmptyBigs, bool exceptExeAndDllsFiles = true)
        {
            if (!string.IsNullOrEmpty(targetFolder) && !Directory.Exists(targetFolder))
            {
                Directory.CreateDirectory(targetFolder);
            }

            var modDirectoryInfo = new DirectoryInfo(sourceFolder);

            var exceptExtensions = new HashSet<string>();

            if (exceptExeAndDllsFiles)
            {
                exceptExtensions = GameLauncher.exceptExtensions;
            }    

            foreach (var file in modDirectoryInfo.GetFiles().Where(f => !exceptExtensions.Contains(Path.GetExtension(f.Name))))
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
                        CreateMirrorForBig(sourceFile, targetFile, createLinksOnEmptyBigs);
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
                    CreateMirrorsFromFolder(folder.FullName, folder.Name, false);
                else
                    CreateMirrorsFromFolder(folder.FullName, targetFolder + "\\" + folder.Name, false);
            }
        }

        public static void CreateMirrorForBig(string sourceFile, string targetFile, bool createLinksOnEmptyBigs)
        {
            var createLink = true;

            if (File.Exists(Path.ChangeExtension(targetFile, "big")))
            {
                if (File.Exists(Path.ChangeExtension(targetFile, "big") + EntryPoint.GenLauncherReplaceSuffix) || (new FileInfo(Path.ChangeExtension(targetFile, "big"))).IsSymbolicLink())
                    File.Delete(Path.ChangeExtension(targetFile, "big"));
                else
                    File.Move(Path.ChangeExtension(targetFile, "big"), Path.ChangeExtension(targetFile, "big") + EntryPoint.GenLauncherReplaceSuffix);

                var info = new FileInfo(sourceFile);

                if (info.Length == 24 && !createLinksOnEmptyBigs)
                    createLink = false;
            }

            if (String.Equals(Path.GetExtension(sourceFile), EntryPoint.GenLauncherReplaceSuffix) && createLink)
            {
                targetFile = Path.ChangeExtension(targetFile, "");
                targetFile = targetFile.Remove(targetFile.Length - 1);
                CreateSymbolicLink(Path.ChangeExtension(targetFile, "big"), sourceFile, SymbolicLink.File);
            }
            else
            {
                if (createLink)
                    CreateSymbolicLink(Path.ChangeExtension(targetFile, "big"), sourceFile, SymbolicLink.File);
            }
                
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
