using Minio;
using Minio.DataModel.Args;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

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

            var useDefaultCredentials = string.IsNullOrEmpty(version.S3HostPublicKey)
                || string.IsNullOrEmpty(version.S3HostSecretKey);
            var publicKey = useDefaultCredentials ? GenInsavePKey : version.S3HostPublicKey;
            var secretKey = useDefaultCredentials ? GenInsaveSKey : version.S3HostSecretKey;

            minioClient = new MinioClient()
                .WithEndpoint(version.S3HostLink)
                .WithCredentials(publicKey, secretKey)
                .Build();

            return await GetFilesFromBucket(version);
        }

        private async Task<List<ModificationFileInfo>> GetFilesFromBucket(ModificationVersion version)
        {
            await minioClient.ListBucketsAsync();

            var filestList = new List<ModificationFileInfo>();
            var listObjectsArgs = new ListObjectsArgs()
                .WithBucket(version.S3BucketName)
                .WithPrefix(version.S3FolderName)
                .WithRecursive(true);
            var objects = minioClient.ListObjectsEnumAsync(listObjectsArgs);
            var enumerator = objects.GetAsyncEnumerator();

            try
            {
                while (await enumerator.MoveNextAsync())
                {
                    var item = enumerator.Current;
                    filestList.Add(new ModificationFileInfo(
                        item.Key.Replace(version.S3FolderName + '/', ""),
                        item.ETag,
                        item.Size));
                }
            }
            finally
            {
                await enumerator.DisposeAsync();
            }

            return filestList;
        }
    }
}
