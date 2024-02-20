using Minio;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Minio.DataModel.Args;

namespace GenLauncherNet
{
    public class S3StorageHandler
    {
        private IMinioClient minioClient;

        public const string GenInsavePKey = "S58TYR9ISEZV8PBP8QG1";
        public const string GenInsaveSKey = "b2RU1oqVU5toJRnb4gODrXX8sBSgoLcHRX6qPWxj";

        public async Task<List<ModificationFileInfo>> GetModInfo(ModificationVersion version)
        {
            var current = new CultureInfo("en-US");
            current.DateTimeFormat = new DateTimeFormatInfo();
            current.DateTimeFormat.Calendar = new GregorianCalendar();

            Thread.CurrentThread.CurrentCulture = current;

            if (string.IsNullOrEmpty(version.S3HostPublicKey) || String.IsNullOrEmpty(version.S3HostSecretKey))
                minioClient = new MinioClient()
                    .WithEndpoint(version.S3HostLink)
                    .WithCredentials(GenInsavePKey, GenInsaveSKey)
                    .Build();
            else
                minioClient = new MinioClient()
                    .WithEndpoint(version.S3HostLink)
                    .WithCredentials(version.S3HostPublicKey, version.S3HostSecretKey)
                    .Build();

            return await GetFilesFromBucket(version);
        }

        private async Task<List<ModificationFileInfo>> GetFilesFromBucket(ModificationVersion version)
        {
            var fileList = new List<ModificationFileInfo>();
            var listObjectsArgs = new ListObjectsArgs()
                .WithBucket(version.S3BucketName)
                .WithPrefix(version.S3FolderName)
                .WithRecursive(true);

            var observable = minioClient.ListObjectsAsync(listObjectsArgs);
            var finished = new TaskCompletionSource<bool>();

            observable.Subscribe(
                item => fileList.Add(new ModificationFileInfo(item.Key.Replace(version.S3FolderName + '/', ""),
                    item.ETag, item.Size)),
                ex => throw ex,
                () => finished.TrySetResult(true)
            );

            await finished.Task;

            return fileList;
        }
    }
}