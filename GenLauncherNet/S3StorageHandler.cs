using Minio;
using Minio.DataModel;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GenLauncherNet
{
    public class S3StorageHandler
    {
        private MinioClient minioClient;

        public const string GenInsavePKey = "S58TYR9ISEZV8PBP8QG1";
        public const string GenInsaveSKey = "b2RU1oqVU5toJRnb4gODrXX8sBSgoLcHRX6qPWxj";

        public async Task<List<ModificationFileInfo>> GetModInfo(ModificationVersion version)
        {
            var current = new CultureInfo("en-US");
            current.DateTimeFormat = new DateTimeFormatInfo();
            current.DateTimeFormat.Calendar = new GregorianCalendar();

            Thread.CurrentThread.CurrentCulture = current;

            if (string.IsNullOrEmpty(version.S3HostPublicKey) || String.IsNullOrEmpty(version.S3HostSecretKey))
                minioClient = new MinioClient(version.S3HostLink, GenInsavePKey, GenInsaveSKey);
            else
                minioClient = new MinioClient(version.S3HostLink, version.S3HostPublicKey, version.S3HostSecretKey);

            return await GetFilesFromBucket(version);
        }

        private async Task<List<ModificationFileInfo>> GetFilesFromBucket(ModificationVersion version)
        {
            var getListBucketsTask = await minioClient.ListBucketsAsync();

            var filestList = new List<ModificationFileInfo>();

            bool finished = false;

            var result = minioClient.ListObjectsAsync(version.S3BucketName, version.S3FolderName, true);

            var subscription = result.Subscribe(
            item =>
            {
                filestList.Add(new ModificationFileInfo(item.Key.Replace(version.S3FolderName + '/', ""), item.ETag, item.Size));
            },
            ex => throw new Exception("Cannot enumerate objects in S3 storage"),
            () => finished = true);


            while (!finished)
            {

            }
            return filestList;
        }
    }
}
