using GenLauncherNet.Utility;
using Minio.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GenLauncherNet
{
    public class S3Updater : IUpdater
    {
        public event Action<long?, long, double?, ModificationContainer, string> ProgressChanged;
        public event Action<ModificationContainer, DownloadResult> Done;
        public DownloadResult DownloadResult;
        public ModificationContainer ModBoxData { get; set; }

        private static HashSet<string> extensionsToCheckHash = new HashSet<string> { ".w3d", ".big", ".bik", ".gib", ".dds", ".tga", ".ini", ".scb", ".wnd", ".csf", ".str", ".bik" };

        string _downloadUrl;
        string _destinationFilePath;
        string _downloadPath;
        string _fileName;

        TimerCallback timerCallback;
        Timer _timer;

        long _bytesRead = 0L;
        long _totalBytesRead = 0L;
        long _readCount = 0L;
        long _timerReadCount = 0L;
        byte[] _buffer = new byte[innerBufferSize];
        bool _isMoreToRead = true;
        HttpResponseMessage _response;
        Stream _contentStream;
        FileStream _fileStream;
        long? _totalDownloadSize;
        long? _downloadSize;
        int _currentAttempt = 0;
        HttpClient _httpClient;
        

        const int innerBufferSize = 8192;
        const int bufferSize = innerBufferSize * 1024 * 4;
        const int connectionAttempts = 5;

        public S3Updater()
        {
        }

        public DownloadReadiness GetDownloadReadiness()
        {
            if (TimeUtility.IsSysTimeOutOfSync())
            {
                return new DownloadReadiness { ReadyToDownload = false, Error = ErrorType.TimeOutOfSync };
            }
            return new DownloadReadiness { ReadyToDownload = true, Error = 0 };
        }

        public void SetModificationInfo(ModificationContainer modification)
        {
            ModBoxData = modification;            
        }

        public async Task StartDownloadModification()
        {
            try
            {
                _totalDownloadSize = null;
                _totalBytesRead = 0;
                _bytesRead = 0;
                _httpClient = new HttpClient();
                var reposFilesInfo = await GetFilesInfoFromS3Storage(ModBoxData);
                var latestInstalledVersion = ModBoxData.ContainerModification.ModificationVersions.OrderBy(v => v).Where(v => v.Installed).LastOrDefault();
                var folderName = GetTempCopyOfFolder();
                await Task.Run(() => CopyUnchangedFiles(folderName, reposFilesInfo, latestInstalledVersion));

                foreach (var fileInfo in reposFilesInfo)
                {
                    if (_totalDownloadSize.HasValue)
                    {
                        _totalDownloadSize = _totalDownloadSize.Value + (long)fileInfo.Size;
                    } else
                    {
                        _totalDownloadSize = (long)fileInfo.Size;
                    }
                }

                foreach (var fileInfo in reposFilesInfo)
                {
                    var fileCheckSuccess = false;
                    while (!fileCheckSuccess)
                    {
                        _fileName = fileInfo.FileName;
                        _downloadSize = (long)fileInfo.Size;
                        _downloadPath = folderName;
                        _destinationFilePath = _downloadPath + "/" + _fileName;

                        if (DownloadResult.Canceled || DownloadResult.Crashed)
                        {
                            this.Dispose();
                            Done(ModBoxData, DownloadResult);
                            return;
                        }

                        _downloadUrl = string.Format("https://{0}/{1}/{2}/{3}", ModBoxData.LatestVersion.S3HostLink.Split(':')[0],
                            ModBoxData.LatestVersion.S3BucketName, ModBoxData.LatestVersion.S3FolderName, fileInfo.FileName);

                        await Download();
                        if (!DownloadResult.Canceled && !DownloadResult.Crashed)
                        {
                            fileCheckSuccess = await CheckFileSuccessDownload(fileInfo);
                        }
                    }
                }

                await Task.Run(() => RenameTempFolder(_downloadPath));
                Done(ModBoxData, DownloadResult);
            }
            catch (UnexpectedMinioException)
            {
                if (DownloadResult.TimedOut)
                {
                    if (_currentAttempt >= connectionAttempts)
                    {
                        Done(ModBoxData, DownloadResult);
                    }
                    else
                    {
                        _currentAttempt++;
                        ModBoxData.SetUIMessages("Connection timed out. Trying to reastablish connection... attempt: " + _currentAttempt);
                        await StartDownloadModification();
                    }
                }
                else
                {
                    this.Dispose();
                    DownloadResult.Crashed = true;
                    DownloadResult.Message = "Unexpected Minio API Exception. Try to sync your system time";
                    return;
                }
            }
            catch (Exception e)
            {
                this.Dispose();

                if (DownloadResult.TimedOut)
                {
                    if (_currentAttempt >= connectionAttempts)
                    {
                        Done(ModBoxData, DownloadResult);
                    }
                    else
                    {
                        _currentAttempt++;
                        ModBoxData.SetUIMessages("Connection timed out. Trying to reastablish connection... attempt: " + _currentAttempt);
                        await StartDownloadModification();
                    }
                }
                else
                {
                    DownloadResult.Crashed = true;

                    if (e.InnerException != null)
                        DownloadResult.Message = e.InnerException.Message;
                    else
                        DownloadResult.Message = e.Message;

                    Done(ModBoxData, DownloadResult);
                }
            }
        }

        private async Task<bool> CheckFileSuccessDownload(ModificationFileInfo fileInfo)
        {
            if (!extensionsToCheckHash.Contains(Path.GetExtension(_fileName).ToLower()))
                return true;
                
            var fileGibName = Path.ChangeExtension(_destinationFilePath, "gib");
            if (File.Exists(_destinationFilePath))
            {
                var hashSum = await Task.Run(() => MD5ChecksumCalculator.ComputeMD5Checksum(_destinationFilePath));
                if (String.Equals(hashSum, fileInfo.Hash, StringComparison.OrdinalIgnoreCase))
                    return true;
                else File.Delete(_destinationFilePath);
            }
            else
            if (File.Exists(fileGibName))
            {
                var hashSum = await Task.Run(() => MD5ChecksumCalculator.ComputeMD5Checksum(fileGibName));
                if (String.Equals(hashSum, fileInfo.Hash, StringComparison.OrdinalIgnoreCase))
                    return true; 
                else File.Delete(fileGibName);
            }
            
            ModBoxData.SetUIMessages("Hash sum mismatch detected, restart file download");
            await Task.Delay(TimeSpan.FromSeconds(5));
            _totalBytesRead = _totalBytesRead - (long)fileInfo.Size;
            return false;
        }

        private async Task Download()
        {
            if (File.Exists(_destinationFilePath))
            {                
                var fi = new FileInfo(_destinationFilePath);
                _bytesRead = fi.Length;
            }
            else
            if (File.Exists(Path.ChangeExtension(_destinationFilePath, "gib")))
            {
                var fi = new FileInfo(Path.ChangeExtension(_destinationFilePath, "gib"));
                _bytesRead = fi.Length;
            }
            else
            {
                _bytesRead = 0;
            }

            var mes = new HttpRequestMessage(HttpMethod.Get, _downloadUrl);

            var leftBytes = _downloadSize - _bytesRead;
            _totalBytesRead += _bytesRead;

            if (leftBytes == 0)
            {                
               return;
            }

            mes.Headers.Add("Range", String.Format("bytes={0}-{1}", _bytesRead, _downloadSize));

            _response = await _httpClient.SendAsync(mes, HttpCompletionOption.ResponseHeadersRead);

            _response.EnsureSuccessStatusCode();
            DownloadResult.TimedOut = false;
            _currentAttempt = 0;

            CreateSubFolderForDownloadingFile(_fileName, _downloadPath);
            if (_fileStream == null) SetFileStream();

            await DownloadFileFromS3HttpResponseMessage();
        }

        private async Task DownloadFileFromS3HttpResponseMessage()
        {
            _contentStream = await _response.Content.ReadAsStreamAsync();                       
            await ProcessS3ContentStream();
        }

        private async Task ProcessS3ContentStream()
        {
            await Task.Run(() => SetTimeoutTimer());

            _isMoreToRead = true;
            do
            {
                int bytesRead;

                if (DownloadResult.Canceled)
                    break;

                bytesRead = await _contentStream.ReadAsync(_buffer, 0, _buffer.Length);

                if (bytesRead == 0)
                {
                    _isMoreToRead = false;
                    continue;
                }

                await _fileStream.WriteAsync(_buffer, 0, bytesRead);

                _totalBytesRead += bytesRead;
                _readCount += 1;


                if (_readCount % 10 == 0)
                    TriggerProgressChanged(_fileName);
            }
            while (_isMoreToRead);

            TriggerProgressChanged(_fileName);

            _fileStream?.Dispose();
            _fileStream = null;
            _contentStream?.Dispose();

            if (String.Equals(Path.GetExtension(_fileName).Replace(".", ""), "big") && !DownloadResult.Canceled)
            {
                if (File.Exists(Path.ChangeExtension(_downloadPath + '/' + _fileName, ".gib")))
                    File.Delete(Path.ChangeExtension(_downloadPath + '/' + _fileName, ".gib"));

                File.Move(_downloadPath + '/' + _fileName, Path.ChangeExtension(_downloadPath + '/' + _fileName, ".gib"));
            }
        }

        private void SetTimeoutTimer()
        {
            var num = 0;
            _timerReadCount = -1;
            timerCallback = new TimerCallback(Count);
            _timer = new Timer(timerCallback, num, 5000, 10000);

        }

        public void Count(object obj)
        {
            if (_readCount == _timerReadCount)
            {
                DownloadResult.TimedOut = true;
                DownloadResult.Message = "Connection timed out. Unable to establish a new connection";
                this.Dispose();
            }
            else
                _timerReadCount = _readCount;
        }

        private void TriggerProgressChanged(string fileName = null)
        {
            if (ProgressChanged == null)
                return;

            double? progressPercentage = null;
            if (_totalDownloadSize.HasValue)
                progressPercentage = Math.Round((double)_totalBytesRead / _totalDownloadSize.Value * 100, 2);

            ProgressChanged(_totalDownloadSize, _totalBytesRead, progressPercentage, ModBoxData, fileName);
        }

        private void RenameTempFolder(string downloadPath)
        {
            var targetPath = downloadPath.Replace(EntryPoint.GenLauncherVersionFolderCopySuffix, "");

            if (Directory.Exists(targetPath))
                Directory.Delete(targetPath);

            Directory.Move(downloadPath, targetPath);
        }

        private void CreateSubFolderForDownloadingFile(string fileName, string downloadPath)
        {
            var subStrings = fileName.Split('/').ToList();

            if (subStrings.Count > 1)
            {
                var exactFileName = subStrings[subStrings.Count - 1];
                var pathToCreate = downloadPath + '/' + fileName.Replace(exactFileName, "");
                Directory.CreateDirectory(pathToCreate);
            }
        }

        private void SetFileStream()
        {
            _fileStream = new FileStream(_destinationFilePath, FileMode.Append, FileAccess.Write, FileShare.None, bufferSize, true);
        }

        public void CancelDownload()
        {
            DownloadResult.Canceled = true;
        }
        

        public DownloadResult GetResult()
        {
            return DownloadResult;
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
            _contentStream?.Dispose();
            _fileStream?.Dispose();
            _fileStream = null;
            _timer?.Dispose();
            _readCount = 0;
        }

        private string GetTempCopyOfFolder()
        {
            var tempFolderName = ModBoxData.LatestVersion.GetFolderName() + EntryPoint.GenLauncherVersionFolderCopySuffix;

            if (!Directory.Exists(tempFolderName))
                Directory.CreateDirectory(tempFolderName);

            return tempFolderName;
        }

        private void GetModFilesInfoFromTempFolder(string prefix, DirectoryInfo directoryInfo, List<ModificationFileInfo> result)
        {
            prefix += directoryInfo.Name + '/';

            foreach (var file in directoryInfo.GetFiles())
            {
                result.Add(new ModificationFileInfo(prefix + file.Name, MD5ChecksumCalculator.ComputeMD5Checksum(file.FullName)));
            }

            foreach (var subDir in directoryInfo.GetDirectories())
            {
                GetModFilesInfoFromTempFolder(prefix, subDir, result);
            }
        }


        private void MoveDirectoryContent(string sourceDir, string destinationDir, bool recursive, List<ModificationFileInfo> reposFiles, string pathAddition)
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

                var fileSize = (ulong)file.Length;

                if (!File.Exists(targetFilePath) && (reposFiles.Contains(new ModificationFileInfo(Path.Combine(pathAddition, file.Name).Replace("\\", "/"), MD5ChecksumCalculator.ComputeMD5Checksum(file.FullName), fileSize)) 
                    || reposFiles.Contains(new ModificationFileInfo(Path.Combine(pathAddition, bigFileName).Replace("\\", "/"), MD5ChecksumCalculator.ComputeMD5Checksum(file.FullName), fileSize))))
                    file.CopyTo(targetFilePath);
            }

            if (recursive)
            {
                foreach (DirectoryInfo subDir in dirs)
                {
                    string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
                    MoveDirectoryContent(subDir.FullName, newDestinationDir, true, reposFiles, Path.Combine(pathAddition, subDir.Name).Replace("\\", "/"));
                }
            }
        }

        private void CopyUnchangedFiles(string folderName, List<ModificationFileInfo> reposFiles, ModificationVersion latestInstalledVersion)
        {
            if (latestInstalledVersion != null)
                MoveDirectoryContent(latestInstalledVersion.GetFolderName(), folderName, true, reposFiles, String.Empty);           
        }

        private async Task<List<ModificationFileInfo>> GetFilesInfoFromS3Storage(ModificationContainer modData)
        {
            var s3StorageHandler = new S3StorageHandler();
            var storageFilesInfo = await s3StorageHandler.GetModInfo(modData.LatestVersion);

            return storageFilesInfo;
        }
    }
}
