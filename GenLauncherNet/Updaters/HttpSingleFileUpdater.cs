using SevenZipExtractor;
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
    public class HttpSingleFileUpdater: IUpdater
    {
        public event Action<long?, long, double?, ModificationContainer, string> ProgressChanged;
        public event Action<ModificationContainer, DownloadResult> Done;
        private DownloadResult DownloadResult;
        public ModificationContainer ModBoxData { get; private set; }

        private string _downloadUrl;
        private string _destinationFilePath;
        private bool _extractionRequers = false;
        private string _downloadPath;
        private string _fileName;

        private TimerCallback timerCallback;
        private Timer timer;

        Int64 _totalBytesRead = 0L;
        Int64 _totalBytesReadOld = 0L;
        long _readCount = 0L;
        byte[] _buffer = new byte[innerBufferSize];
        bool _isMoreToRead = true;

        HttpResponseMessage _response;
        Stream _contentStream;
        FileStream _fileStream;
        long? _totalDownloadSize;

        private HttpClient _httpClient;

        const int bufferSize = 8192 * 1024;
        const int innerBufferSize = 8192;

        public HttpSingleFileUpdater()
        {
            _httpClient = new HttpClient();
        }

        public void SetModificationInfo(ModificationContainer modification)
        {
            ModBoxData = modification;                  
        } 

        public async Task StartDownloadModification()
        {
            try
            {
                _fileName = await GetFileName();
                _downloadPath = GetTempCopyOfFolder();
                _destinationFilePath = _downloadPath + "\\" + _fileName;
                SetStartByte();

                DownloadResult.Canceled = false;             
                do
                {
                    var mes = new HttpRequestMessage(HttpMethod.Get, _downloadUrl);
                   

                    mes.Headers.Add("Range", String.Format("bytes={0}-{1}", DownloadResult.BytesRead, DownloadResult.BytesRead + bufferSize));

                    _response = await _httpClient.SendAsync(mes);

                    _response.EnsureSuccessStatusCode();

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

                if (_extractionRequers)
                {
                    this.Dispose();
                    await Task.Run(() => ExtractArchive());
                    DeleteTempFile();
                }

                RemoveTempFolder();

                Done(ModBoxData, DownloadResult);
            }
            catch (Exception e)
            {
                this.Dispose();

                if (DownloadResult.TimedOut)
                {
                    Done(ModBoxData, DownloadResult);
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

        private async Task<string> GetFileName()
        {
            _downloadUrl = DownloadsLinkParser.ParseDownloadLink(ModBoxData.ContainerModification.SimpleDownloadLink);
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls | SecurityProtocolType.Ssl3;

            var mes = new HttpRequestMessage(HttpMethod.Get, _downloadUrl);

            _response = await _httpClient.SendAsync(mes);

            _response.EnsureSuccessStatusCode();

            string fileName;

            if (_response.Content.Headers.ContentDisposition == null)
                throw new Exception("Download link is incorrect, please contact modification creator and try again later.");

            if (_response.Content.Headers.ContentDisposition.FileName != null)
                fileName = _response.Content.Headers.ContentDisposition.FileName.Replace("\"", String.Empty).Replace("\\", String.Empty);
            else
                fileName = _response.Content.Headers.ContentDisposition.FileNameStar;

            return fileName;
        }

        private void SetFileStream()
        {
            var extension = Path.GetExtension(_destinationFilePath).Replace(".", "");

            if (extension == "zip" || extension == "rar" || extension == "7z")
                _extractionRequers = true;

            _fileStream = new FileStream(_destinationFilePath, FileMode.Append, FileAccess.Write, FileShare.None, innerBufferSize, true);
        }

        private void SetStartByte()
        {
            if (File.Exists(_destinationFilePath))
            {
                var fi = new FileInfo(_destinationFilePath);
                DownloadResult.BytesRead = fi.Length;
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

            //await Task.Run(() => SetTimeoutTimer());
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
            if (_totalDownloadSize.HasValue)
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
            timerCallback = new TimerCallback(Count);
            timer = new Timer(timerCallback, num, 5000, 30000);
        }

        public void Count(object obj)
        {
            if (_totalBytesReadOld == _totalBytesRead)
            {
                DownloadResult.TimedOut = true;
                DownloadResult.Message = "Connection timed out";
                this.Dispose();
            }
            else
                _totalBytesReadOld = _totalBytesRead;
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
            _contentStream?.Dispose();
            _fileStream?.Dispose();
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
