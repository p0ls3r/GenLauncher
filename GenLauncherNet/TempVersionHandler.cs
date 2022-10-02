using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace GenLauncherNet
{    
    public class TempVersionHandler
    {
        private HashSet<ModificationFileInfo> filesToCopy;
        private HashSet<string> fileNamesToCopy = new HashSet<string>();
        private List<ModificationFileInfo> filesToDownload;
        private ModificationVersion ReposModification;
        private ModificationVersion LatestInstalledVersion;

        //TODO refcoring - the method does two things
        public async Task DownloadFilesInfoFromS3Storage(ModificationContainer modData)
        {
            ReposModification = modData.LatestVersion;
            LatestInstalledVersion = modData.ContainerModification.ModificationVersions.OrderBy(v => v).Where(v => v.Installed).LastOrDefault();

            var localFilesInfo = await Task.Run(() => GetModFilesInfoFromLocalMod(LatestInstalledVersion));

            var s3StorageHandler = new S3StorageHandler();
            var storageFilesInfo = await s3StorageHandler.GetModInfo(modData.LatestVersion);

            filesToCopy = new HashSet<ModificationFileInfo>();

            filesToDownload = new List<ModificationFileInfo>();
            filesToCopy = new HashSet<ModificationFileInfo>();

            foreach (var storageFileInfo in storageFilesInfo)
            {
                if (localFilesInfo.Contains(storageFileInfo))
                    filesToCopy.Add(storageFileInfo);
                else
                    filesToDownload.Add(storageFileInfo);
            }

            foreach (var fileInfo in filesToCopy)
            {
                var fileNameSplit = fileInfo.FileName.Split('/');

                fileNamesToCopy.Add(fileNameSplit[fileNameSplit.Length - 1]);
            }
        }

        public List<ModificationFileInfo> GetFilesToDownload()
        {
            if (filesToDownload != null)
                return filesToDownload;

            throw new Exception("Need to define files to download before requesting them");
        }

        public string CreateTempCopyOfFolder()
        {
            var tempFolderName = ReposModification.GetFolderName() + EntryPoint.GenLauncherVersionFolderCopySuffix;

            if (LatestInstalledVersion != null && filesToCopy.Count > 0)
                CopyDirectory(LatestInstalledVersion.GetFolderName(), tempFolderName, true);
            else
                Directory.CreateDirectory(tempFolderName);

            return tempFolderName;
        }


        private List<ModificationFileInfo> GetModFilesInfoFromLocalMod(ModificationVersion latestVersion)
        {
            var result = new List<ModificationFileInfo>();

            DirectoryInfo directoryInfo;

            if (Directory.Exists(ReposModification.GetFolderName() + EntryPoint.GenLauncherVersionFolderCopySuffix))
                directoryInfo = new DirectoryInfo(ReposModification.GetFolderName() + EntryPoint.GenLauncherVersionFolderCopySuffix);
            else
            {
                if (latestVersion == null)
                    return result;
                else
                    directoryInfo = new DirectoryInfo(latestVersion.GetFolderName());
            }

            foreach (var file in directoryInfo.GetFiles())
            {
                result.Add(new ModificationFileInfo(file.Name, ComputeMD5Checksum(file.FullName)));
            }

            foreach (var subDir in directoryInfo.GetDirectories())
            {
                GetModFilesInfoFromTempFolder(String.Empty, subDir, result);
            }

            return result;

        }
        
        private void GetModFilesInfoFromTempFolder(string prefix, DirectoryInfo directoryInfo, List<ModificationFileInfo> result)
        {
            prefix += directoryInfo.Name + '/';

            foreach (var file in directoryInfo.GetFiles())
            {
                result.Add(new ModificationFileInfo(prefix + file.Name, ComputeMD5Checksum(file.FullName)));
            }

            /*if (!String.Equals(directoryInfo.Name + '/', prefix, StringComparison.OrdinalIgnoreCase))
                prefix += directoryInfo.Name + '/';*/

            foreach (var subDir in directoryInfo.GetDirectories())
            {
                //prefix += subDir.Name + '/';

                GetModFilesInfoFromTempFolder(prefix, subDir, result);
            }
        }

        private static string ComputeMD5Checksum(string path)
        {
            using (FileStream fs = System.IO.File.OpenRead(path))
            {
                var md5 = new MD5CryptoServiceProvider();
                byte[] fileData = new byte[fs.Length];
                fs.Read(fileData, 0, (int)fs.Length);
                byte[] checkSum = md5.ComputeHash(fileData);
                string result = BitConverter.ToString(checkSum).Replace("-", String.Empty);
                return result;
            }
        }

        private void CopyDirectory(string sourceDir, string destinationDir, bool recursive)
        {
            var dir = new DirectoryInfo(sourceDir);

            DirectoryInfo[] dirs = dir.GetDirectories();

            Directory.CreateDirectory(destinationDir);

            foreach (FileInfo file in dir.GetFiles())
            {
                string targetFilePath = Path.Combine(destinationDir, file.Name);

                var bigFileName = String.Empty;

                if (Path.GetExtension(file.Name).Replace(".", string.Empty) == "gib")
                    bigFileName = Path.ChangeExtension(file.Name, ".big");

                if (!File.Exists(targetFilePath) && (fileNamesToCopy.Contains(file.Name) || fileNamesToCopy.Contains(bigFileName)))
                    file.CopyTo(targetFilePath);
            }

            if (recursive)
            {
                foreach (DirectoryInfo subDir in dirs)
                {
                    string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
                    CopyDirectory(subDir.FullName, newDestinationDir, true);
                }
            }
        }
    }
}
