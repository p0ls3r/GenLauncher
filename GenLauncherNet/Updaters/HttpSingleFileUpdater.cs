using SevenZipExtractor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GenLauncherNet
{
    public class HttpSingleFileUpdater: IUpdater
    {
        public event Action<long?, long, double?, ModificationContainer, string> ProgressChanged;
        public event Action<ModificationContainer, DownloadResult> Done;
        public ModificationContainer ModBoxData { get; private set; }

        string _downloadUrl;
        string _destinationFilePath;
        bool _extractionRequers;
        string _downloadPath;
        string _fileName;

        TimerCallback timerCallback;
        Timer _timer;

        DownloadResult DownloadResult;

        long _totalBytesRead = 0L;
        long _readCount = 0L;
        long _timerReadCount = 0L;
        byte[] _buffer = new byte[innerBufferSize];
        bool _isMoreToRead = true;
        bool _partDownload;
        HttpResponseMessage _response;
        Stream _contentStream;
        FileStream _fileStream;
        long? _chunkDownloadSize;
        long? _totalDownloadSize;
        int _currentAttempt = 0;
        HttpClient _httpClient;
        
        const int innerBufferSize = 8192;
        const int bufferSize = innerBufferSize * 1024 * 4;
        const int connectionAttempts = 5;

        public HttpSingleFileUpdater()
        {            
        }

        public void SetModificationInfo(ModificationContainer modification)
        {
            ModBoxData = modification;                  
        } 

        public async Task StartDownloadModification()
        {
            try
            {
                if (DownloadResult.Canceled)
                {
                    return;
                }

                _httpClient = new HttpClient();
                await GetFileInfo();
                _downloadPath = GetTempCopyOfFolder();
                _destinationFilePath = _downloadPath + "\\" + _fileName;
                SetStartInfo();
               
                if (_partDownload)
                {
                    await PartDownload();
                } else
                {
                    //the old downloading algorithm has been retained to avoid a large number of requests
                    await FullDownload();
                }

                if (!DownloadResult.Canceled && !DownloadResult.Crashed)
                {
                    if (_extractionRequers)
                    {
                        this.Dispose();
                        await Task.Run(() => ExtractArchive());
                        DeleteTempFile();
                    }

                    RemoveTempFolder();
                    DownloadResult.Canceled = false;
                    DownloadResult.Crashed = false;
                }
                Done(ModBoxData, DownloadResult);
            }
            catch (Exception e)
            {
                this.Dispose();

                if (DownloadResult.TimedOut)
                {
                    if (_currentAttempt >= connectionAttempts)
                    {                        
                        Done(ModBoxData, DownloadResult);
                    } else
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

                    Console.WriteLine("Crashed");
                    Done(ModBoxData, DownloadResult);
                }
            }
        }

        private async Task PartDownload()
        {
            do
            {
                if (DownloadResult.Canceled)
                    break;

                var mes = new HttpRequestMessage(HttpMethod.Get, _downloadUrl);

                mes.Headers.Add("Range", String.Format("bytes={0}-{1}", DownloadResult.BytesRead, DownloadResult.BytesRead + bufferSize));

                _response = await _httpClient.SendAsync(mes, HttpCompletionOption.ResponseHeadersRead);

                _response.EnsureSuccessStatusCode();
                DownloadResult.TimedOut = false;
                _currentAttempt = 0;

                if (_fileStream == null) SetFileStream();

                if (DownloadResult.TotalSize == 0L)
                {
                    foreach (var h in _response.Content.Headers)
                        if (String.Equals(h.Key, "Content-Range"))
                        {
                            var rawString = h.Value.FirstOrDefault();
                            DownloadResult.TotalSize = Int64.Parse(rawString.Split('/')[1]);
                            break;
                        }
                }

                await DownloadFileFromHttpResponseMessage();
                DownloadResult.BytesRead += bufferSize + 1;
            }
            while (DownloadResult.BytesRead < DownloadResult.TotalSize);
        }

        private async Task FullDownload()
        {
            _response = await _httpClient.GetAsync(_downloadUrl, HttpCompletionOption.ResponseHeadersRead);

            _response.EnsureSuccessStatusCode();
            DownloadResult.TimedOut = false;
            _currentAttempt = 0;

            if (_fileStream == null) SetFileStream();

            await DownloadFileFromHttpResponseMessage();
        }

        private void RemoveTempFolder()
        {
            if (Directory.Exists(ModBoxData.LatestVersion.GetFolderName()))
                Directory.Delete(ModBoxData.LatestVersion.GetFolderName(), true);

            Directory.Move(_downloadPath, ModBoxData.LatestVersion.GetFolderName());
        }

        private string GetTempCopyOfFolder()
        {
            var tempFolderName = ModBoxData.LatestVersion.GetFolderName() + EntryPoint.GenLauncherVersionFolderCopySuffix;

            if (!Directory.Exists(tempFolderName))
                Directory.CreateDirectory(tempFolderName);

            return tempFolderName;
        }

        private async Task GetFileInfo()
        {
            _downloadUrl = DownloadsLinkParser.ParseDownloadLink(ModBoxData.ContainerModification.SimpleDownloadLink);
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls | SecurityProtocolType.Ssl3;

            var mes = new HttpRequestMessage(HttpMethod.Get, _downloadUrl);

            _response = await _httpClient.SendAsync(mes, HttpCompletionOption.ResponseHeadersRead);

            _response.EnsureSuccessStatusCode();

            if (_response.Content.Headers.ContentDisposition == null)
                throw new Exception("Download link is incorrect, please contact modification creator and try again later.");

            if (_response.Content.Headers.ContentDisposition.FileName != null)
                _fileName = _response.Content.Headers.ContentDisposition.FileName.Replace("\"", String.Empty).Replace("\\", String.Empty);
            else
                _fileName = _response.Content.Headers.ContentDisposition.FileNameStar;

            _totalDownloadSize = _response.Content.Headers.ContentLength;
        }

        private void SetFileStream()
        {
            var extension = Path.GetExtension(_destinationFilePath).Replace(".", "");

            if (extension == "zip" || extension == "rar" || extension == "7z")
                _extractionRequers = true;

            _fileStream = new FileStream(_destinationFilePath, FileMode.Append, FileAccess.Write, FileShare.None, innerBufferSize, true);
        }

        private void SetStartInfo()
        {
            if (File.Exists(_destinationFilePath))
            {
                _partDownload = true;
                var fi = new FileInfo(_destinationFilePath);
                DownloadResult.BytesRead = fi.Length;
                _totalBytesRead = fi.Length;
            }
        }

        private async Task DownloadFileFromHttpResponseMessage()
        {
            _response.EnsureSuccessStatusCode();         

            if (!Directory.Exists(_downloadPath))
                Directory.CreateDirectory(_downloadPath);
            _destinationFilePath = _downloadPath + "\\" + _fileName;

            var extension = Path.GetExtension(_destinationFilePath).Replace(".", "");

            if (extension == "zip" || extension == "rar" || extension == "7z")
                _extractionRequers = true;

            _chunkDownloadSize = _response.Content.Headers.ContentLength;

            _contentStream = await _response.Content.ReadAsStreamAsync();            

            await Task.Run(() => SetTimeoutTimer());
            await ProcessContentStream();
        }

        private async Task ProcessContentStream()
        {
            _isMoreToRead = true;
            do
            {
                if (DownloadResult.Canceled)
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

            if (DownloadResult.Canceled)
            {
                this.Dispose();
                DownloadResult.Message = "Download Canceled";
                Done(ModBoxData, DownloadResult);
                return;
            }

            TriggerProgressChanged();         
        }

        private void TriggerProgressChanged(string fileName = null)
        {
            if (ProgressChanged == null)
                return;

            double? progressPercentage = null;
            if (_chunkDownloadSize.HasValue)
                progressPercentage = Math.Round((double)_totalBytesRead / _totalDownloadSize.Value * 100, 2);

            ProgressChanged(_totalDownloadSize, _totalBytesRead, progressPercentage, ModBoxData, fileName);
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
                archiveFile.Extract(entry => {
                    if (Path.GetExtension(entry.FileName).Replace(".", "") == "big")
                    {
                        return Path.Combine(_downloadPath + "\\" + Path.ChangeExtension(entry.FileName, "gib"));
                    }
                    else
                    {
                        return _downloadPath + "\\" + entry.FileName;
                    }
                });
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

        public void Dispose()
        {
            _httpClient?.Dispose();
            _contentStream?.Dispose();
            _fileStream?.Dispose();
            _fileStream = null;
            _timer.Dispose();
            _readCount = 0;
        }

        public void CancelDownload()
        {
            DownloadResult.Canceled = true;
        }

        public DownloadResult GetResult()
        {
            return DownloadResult;
        }
    }
}
