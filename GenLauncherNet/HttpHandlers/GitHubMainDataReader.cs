using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Serialization;
using YamlDotNet.Serialization;

namespace GenLauncherNet
{
    public class GitHubMainDataReader : IDisposable
    {
        private ReposModsData _data;
        private HttpClient _httpClient;

        public GitHubMainDataReader(ReposModsData data)
        {
            _data = data;
            _httpClient = new HttpClient { Timeout = TimeSpan.FromDays(1) };
            _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("GenLauncher", Assembly.GetExecutingAssembly().GetName().Version.ToString()));
        }

        public List<string> GetReposModsNames()
        {
            return _data.modDatas.Select(t => t.ModName).ToList();
        }

        public async Task<Dictionary<ModificationReposVersion, ModAddonsAndPatches>> UpdateDownloadedModsDataFromRepos(List<string> downloadedMods)
        {
            var mods = new Dictionary<ModificationReposVersion, ModAddonsAndPatches>();

            foreach (var modData in _data.modDatas)
            {
                if (!String.IsNullOrEmpty(modData.ModName) && !downloadedMods.Contains(modData.ModName.ToLower()))
                    continue;

                try
                {
                    var kvp = await DownloadModData(modData);
                    mods.Add(kvp.Key, kvp.Value);
                }
                catch
                {
                    //TODO logger
                    continue;
                }
            }

            return mods;
        }

        public async Task<KeyValuePair<ModificationReposVersion, ModAddonsAndPatches>> DownloadModData(ModAddonsAndPatches modData)
        {
            ModificationReposVersion modification = null;

            using (var response = await _httpClient.GetAsync(modData.ModLink, HttpCompletionOption.ResponseHeadersRead))
                modification = await DownloadDataFromHttpResponseMessage(response);

            return new KeyValuePair<ModificationReposVersion, ModAddonsAndPatches>(modification, modData);
        }

        public async Task<KeyValuePair<ModificationReposVersion, ModAddonsAndPatches>> DownloadModDataByName(string Name)
        {
            var modData = _data.modDatas.Where(t => String.Equals(t.ModName.ToLower(), Name.ToLower(), StringComparison.OrdinalIgnoreCase)).FirstOrDefault();

            return await DownloadModData(modData);
        }

        public async Task<List<ModificationReposVersion>> ReadGlobalAddons()
        {
            var mods = new List<ModificationReposVersion>();

            foreach (var addonUrl in _data.globalAddonsData)
            {
                try
                {
                    ModificationReposVersion modification = null;

                    using (var response = await _httpClient.GetAsync(addonUrl, HttpCompletionOption.ResponseHeadersRead))
                        modification = await DownloadDataFromHttpResponseMessage(response);

                    mods.Add(modification);
                }
                catch
                {
                    //TODO logger
                    continue;
                }
            }

            return mods;
        }

        public async Task<List<ModificationReposVersion>> ReadAddonsForMod(List<string> urls)
        {
            var result = new List<ModificationReposVersion>();

            foreach (var url in urls)
            {
                try
                {
                    ModificationReposVersion modification = null;

                    using (var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
                        modification = await DownloadDataFromHttpResponseMessage(response);

                    result.Add(modification);
                }
                catch
                {
                    //TODO logger
                    continue;
                }
            }

            return result;
        }

        private async Task<ModificationReposVersion> DownloadDataFromHttpResponseMessage(HttpResponseMessage response)
        {
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength;

            using (var contentStream = await response.Content.ReadAsStreamAsync())
                return await ProcessContentStream(totalBytes, contentStream);
        }

        private async Task<ModificationReposVersion> ProcessContentStream(long? totalDownloadSize, Stream contentStream)
        {
            var totalBytesRead = 0L;
            var readCount = 0L;
            var buffer = new byte[8192];
            var isMoreToRead = true;
            using (MemoryStream memStream = new MemoryStream())
            {
                do
                {
                    var bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length);

                    if (bytesRead == 0)
                    {
                        isMoreToRead = false;
                        continue;
                    }

                    await memStream.WriteAsync(buffer, 0, bytesRead);

                    totalBytesRead += bytesRead;
                    readCount += 1;


                }
                while (isMoreToRead);

                memStream.Seek(0, SeekOrigin.Begin);

                var deSerializer = new Deserializer();

                var modification = deSerializer.Deserialize<ModificationReposVersion>(new StreamReader(memStream));

                return modification;
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}
