using GenLauncherNet;
using SevenZipExtractor;
using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GenLauncherNet
{
    public class ContentDownloader : IDisposable
    {
        private readonly string _downloadUrl;
        private string _destinationFilePath;
        private readonly CancellationToken? _cancellationToken;
        private string _tempFilePrefix = "";
        private bool _extractionRequers;

        private HttpClient _httpClient;
        public event Action<long?, long, int> ProgressChanged;
        public event Action Done;

        private Action<string> _endingAction;

        public ContentDownloader(string downloadUrl, Action<string> action, string tempPrefix, CancellationToken? cancellationToken = null, bool extractionRequers = true)
        {
            _downloadUrl = downloadUrl;
            _cancellationToken = cancellationToken;
            _endingAction = action;
            _tempFilePrefix = tempPrefix;
            _extractionRequers = extractionRequers;
        }

        public async Task StartDownload()
        {
            _httpClient = new HttpClient { Timeout = TimeSpan.FromDays(1) };

            using (var response = await _httpClient.GetAsync(_downloadUrl, HttpCompletionOption.ResponseHeadersRead))
                await DownloadFileFromHttpResponseMessage(response);
        }

        private async Task DownloadFileFromHttpResponseMessage(HttpResponseMessage response)
        {
            response.EnsureSuccessStatusCode();

            var fileName = String.Empty;

            if (String.IsNullOrEmpty(_destinationFilePath) && response.Content.Headers.ContentDisposition != null)
            {
                fileName = response.Content.Headers.ContentDisposition.FileName.Replace("\"", String.Empty).Replace("\\", String.Empty);
            }
            else
            {
                fileName = "GenLauncherDownloadingFile";
            }
            _destinationFilePath = fileName + _tempFilePrefix;

            var totalBytes = response.Content.Headers.ContentLength;

            using (var contentStream = await response.Content.ReadAsStreamAsync())
                await ProcessContentStream(totalBytes, contentStream);
        }

        private async Task ProcessContentStream(long? totalDownloadSize, Stream contentStream)
        {
            var totalBytesRead = 0L;
            var readCount = 0L;
            var buffer = new byte[8192];
            var isMoreToRead = true;

            using (var fileStream = new FileStream(_destinationFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
            {
                do
                {
                    int bytesRead;
                    if (_cancellationToken.HasValue)
                    {
                        bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, _cancellationToken.Value);
                    }
                    else
                    {
                        bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length);
                    }

                    if (bytesRead == 0)
                    {
                        isMoreToRead = false;
                        continue;
                    }

                    await fileStream.WriteAsync(buffer, 0, bytesRead);

                    totalBytesRead += bytesRead;
                    readCount += 1;

                    if (readCount % 10 == 0)
                        TriggerProgressChanged(totalDownloadSize, totalBytesRead);
                }
                while (isMoreToRead);

            }

            TriggerProgressChanged(totalDownloadSize, totalBytesRead);

            var fileName = "GenLauncherDownloadingFile";

            if (_extractionRequers)
               fileName = ExtractFileFromArchieve(_destinationFilePath);

            if (_endingAction != null)
                _endingAction(fileName);

            Done?.Invoke();
        }

        private string ExtractFileFromArchieve(string _destinationFilePath)
        {
            var tempFileName = String.Empty;
            using (var archiveFile = new ArchiveFile(_destinationFilePath))
            {
                foreach (Entry entry in archiveFile.Entries)
                {
                    entry.Extract(entry.FileName + _tempFilePrefix);
                    tempFileName = entry.FileName + _tempFilePrefix;
                }
            }

            File.Delete(_destinationFilePath);

            return tempFileName;
        }

        private void TriggerProgressChanged(long? totalDownloadSize, long totalBytesRead)
        {
            if (ProgressChanged == null)
                return;

            int progressPercentage = 0;
            if (totalDownloadSize.HasValue)
                progressPercentage = Convert.ToInt32(Math.Round((double)totalBytesRead / totalDownloadSize.Value * 100, 2));

            ProgressChanged(totalDownloadSize, totalBytesRead, progressPercentage);
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}
