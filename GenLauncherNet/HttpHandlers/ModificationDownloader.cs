using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using SevenZipExtractor;

namespace GenLauncherNet
{
    public struct DownloadResult
    {
        public bool Crashed;
        public bool Canceled;
        public bool TimedOut;
        public string Message;
    }

    public class ModificationDownloader : IDisposable
    {
        public DownloadResult Result;

        private string _downloadUrl;
        private string _destinationFilePath;
        private bool _extractionRequers = false;
        private string _downloadPath;

        private TimerCallback timerCallback;
        private Timer timer;

        Int64 _totalBytesRead = 0L;
        Int64 _totalBytesReadOld = 0L;
        long _readCount = 0L;
        byte[] _buffer = new byte[8192];
        bool _isMoreToRead = true;

        HttpResponseMessage _response;
        Stream _contentStream;
        FileStream _fileStream;
        long? _totalDownloadSize;

        ModBoxData _ModBoxData;
        private HttpClient _httpClient;

        public event Action<long?, long, double?, ModBoxData, string> ProgressChanged;
        public event Action<ModBoxData, DownloadResult> Done;

        public ModificationDownloader(ModBoxData modBoxData)
        {
            _ModBoxData = modBoxData;

            _downloadPath = modBoxData.LatestVersion.GetFolderName();
            _httpClient = new HttpClient();
        }

        #region SimpleDownload

        public async Task StartSimpleDownload()
        {
            try
            {
                _downloadUrl = DownloadsLinkParser.ParseDownloadLink(_ModBoxData.ModBoxModification.SimpleDownloadLink);
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls | SecurityProtocolType.Ssl3;
                _response = await _httpClient.GetAsync(_downloadUrl, HttpCompletionOption.ResponseHeadersRead);
                await DownloadFileFromHttpResponseMessage();
            }
            catch (Exception e)
            {
                this.Dispose();
                DeleteTempFile();

                if (Result.TimedOut)
                    Done(_ModBoxData, Result);
                else
                {
                    Result.Crashed = true;

                    if (e.InnerException != null)
                        Result.Message = e.InnerException.Message;
                    else
                        Result.Message = e.Message;
                    
                    Done(_ModBoxData, Result);
                }
            }
        }

        private async Task DownloadFileFromHttpResponseMessage()
        {
            _response.EnsureSuccessStatusCode();

            string fileName;

            if (_response.Content.Headers.ContentDisposition == null)
                throw new Exception("Download link is incorrect, please contact modification creator and try again later.");

            if (_response.Content.Headers.ContentDisposition.FileName != null)
                fileName = _response.Content.Headers.ContentDisposition.FileName.Replace("\"", String.Empty).Replace("\\", String.Empty);
            else
                fileName = _response.Content.Headers.ContentDisposition.FileNameStar;

            if (!Directory.Exists(_downloadPath))
                Directory.CreateDirectory(_downloadPath);
            _destinationFilePath = _downloadPath + "\\" + fileName;

            var extension = Path.GetExtension(_destinationFilePath).Replace(".", "");

            if (extension == "zip" || extension == "rar" || extension == "7z")
                _extractionRequers = true;

            _totalDownloadSize = _response.Content.Headers.ContentLength;

            _contentStream = await _response.Content.ReadAsStreamAsync();
            _fileStream = new FileStream(_destinationFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);            

            await Task.Run(() => SetTimeoutTimer());
            await ProcessContentStream();
        }

        private async Task ProcessContentStream()
        {
            do
            {
                if (Result.Canceled)
                    break;

                int bytesRead;

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
                    TriggerProgressChanged();
            }
            while (_isMoreToRead);

            if (Result.Canceled)
            {
                this.Dispose();
                Result.Message = "Download Canceled";
                Done(_ModBoxData, Result);
                DeleteTempFile();
                return;
            }

            TriggerProgressChanged();
            this.Dispose();

            if (_extractionRequers)
            {
                await Task.Run(() => ExtractArchive());
                DeleteTempFile();
            }

            Done(_ModBoxData, Result);
        }

        private void DeleteTempFile()
        {
            if (File.Exists(_destinationFilePath))
                File.Delete(_destinationFilePath);
        }

        private void ExtractArchive()
        {
            using (var archiveFile = new ArchiveFile(_destinationFilePath))
            {
                foreach (Entry entry in archiveFile.Entries)
                {
                    if (Path.GetExtension(entry.FileName).Replace(".", "") == "big")
                    {
                        entry.Extract(_downloadPath + "\\" + Path.ChangeExtension(entry.FileName, "gib"));
                    }
                    else
                        entry.Extract(_downloadPath + "\\" + entry.FileName);
                }
            }
        }

        #endregion

        #region S3StorageDownload

        public async Task<DownloadResult> StartS3Download(List<ModificationFileInfo> modFiles, string downloadPath)
        {
            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls | SecurityProtocolType.Ssl3;

                var ReposModification = _ModBoxData.LatestVersion;

                _totalDownloadSize = 0;

                foreach (var file in modFiles)
                {
                    _totalDownloadSize += (long)file.Size;
                }

                foreach (var file in modFiles)
                {
                    var downloadURL = string.Format("https://{0}/{1}/{2}/{3}", ReposModification.S3HostLink.Split(':')[0],
                        ReposModification.S3BucketName, ReposModification.S3FolderName, file.FileName);

                    _response = await _httpClient.GetAsync(downloadURL, HttpCompletionOption.ResponseHeadersRead);
                    await DownloadFileFromS3HttpResponseMessage(file.FileName, downloadPath);

                    if (Result.Canceled || Result.Crashed || Result.TimedOut)
                    {
                        this.Dispose();
                        Done(_ModBoxData, Result);
                        return Result;
                    }
                }

                this.Dispose();

                await Task.Run(() => RenameTempFolder(downloadPath));
                Done(_ModBoxData, Result);
            }
            catch (Exception e)
            {
                this.Dispose();

                if (Result.TimedOut)
                    Done(_ModBoxData, Result);
                else
                {
                    Result.Crashed = true;

                    if (e.InnerException != null)
                        Result.Message = e.InnerException.Message;
                    else
                        Result.Message = e.Message;

                    Done(_ModBoxData, Result);
                }
            }

            return Result;
        }

        private async Task DownloadFileFromS3HttpResponseMessage(string fileName, string downloadPath)
        {
            _response.EnsureSuccessStatusCode();
            _contentStream = await _response.Content.ReadAsStreamAsync();
            CreateSubFolderForDownloadingFile(fileName, downloadPath);
            _fileStream = new FileStream(downloadPath + '/' + fileName, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);
            await ProcessS3ContentStream(fileName, downloadPath);
        }

        private async Task ProcessS3ContentStream(string fileName, string downloadPath)
        {
            await Task.Run(() => SetTimeoutTimer());

            _isMoreToRead = true;
            do
            {
                int bytesRead;

                if (Result.Canceled)
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
                    TriggerProgressChanged(fileName);
            }
            while (_isMoreToRead);

            TriggerProgressChanged(fileName);

            _fileStream?.Dispose();
            _contentStream?.Dispose();

            if (String.Equals(Path.GetExtension(fileName).Replace(".", ""), "big"))
            {
                if (File.Exists(Path.ChangeExtension(downloadPath + '/' + fileName, ".gib")))
                    File.Delete(Path.ChangeExtension(downloadPath + '/' + fileName, ".gib"));

                File.Move(downloadPath + '/' + fileName, Path.ChangeExtension(downloadPath + '/' + fileName, ".gib"));
            }
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

            var tempFolders = downloadPath;

            if (subStrings.Count > 1)
            {
                var exactFileName = subStrings[subStrings.Count - 1];
                var pathToCreate = downloadPath + '/' + fileName.Replace(exactFileName, "");
                Directory.CreateDirectory(pathToCreate);
            }
        }

        #endregion

        #region Timer
        private void SetTimeoutTimer()
        {
            var num = 0;
            timerCallback = new TimerCallback(Count);
            timer = new Timer(timerCallback, num, 5000, 30000);
        }

        public void Count(object obj)
        {
            if (_totalBytesReadOld == _totalBytesRead)
            {
                Result.TimedOut = true;
                Result.Message = "Connection timed out";
                this.Dispose();
            }
            else
                _totalBytesReadOld = _totalBytesRead;
        }

        #endregion

        #region Events
        private void TriggerProgressChanged(string fileName = null)
        {
            if (ProgressChanged == null)
                return;

            double? progressPercentage = null;
            if (_totalDownloadSize.HasValue)
                progressPercentage = Math.Round((double)_totalBytesRead / _totalDownloadSize.Value * 100, 2);

            ProgressChanged(_totalDownloadSize, _totalBytesRead, progressPercentage, _ModBoxData, fileName);
        }

        public void CancelDownload()
        {
            Result.Canceled = true;

            this.Dispose();
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
            _contentStream?.Dispose();
            _fileStream?.Dispose();
        }
        #endregion
    }
}
