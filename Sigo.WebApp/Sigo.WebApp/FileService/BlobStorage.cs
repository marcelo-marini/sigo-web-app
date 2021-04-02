﻿using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Polly;

namespace Sigo.WebApp.FileService
{
    public class BlobStorage : IFileService
    {
        private readonly CloudStorageAccount _storageAccount;
        private const string Directory = "usagereport";
        private readonly string _container;

        public BlobStorage(IConfiguration configuration)
        {
            _storageAccount =
                CloudStorageAccount.Parse(configuration.GetSection("AzureBlob").GetValue<string>("Connection"));
            _container = $"{configuration.GetSection("AzureBlob").GetValue<string>("Container")}";
        }

        public async Task<string> UploadAsync(IFormFile file, string code)
        {
            var filePath = await CreateLocalFile(file, code);

            try
            {
                var result = await Policy<string>
                    .Handle<Exception>()
                    .WaitAndRetryAsync(10, i => TimeSpan.FromMilliseconds(10 * i))
                    .ExecuteAsync(async () =>
                    {
                        var sasToken = BuildAccountSasToken();

                        var container = _storageAccount.CreateCloudBlobClient().GetContainerReference(_container);

                        //   var directory = container.GetDirectoryReference(Directory);

                        var blob = container.GetBlockBlobReference(filePath);
                        blob.Properties.ContentType = file.ContentType;
                        blob.Properties.ContentDisposition = $"inline;filename={blob.Name}";

                        await blob.UploadFromFileAsync(filePath);

                        return $"{blob.Uri.AbsoluteUri}{sasToken}";
                    });

                return result;
            }
            finally
            {
                DeleteFile(filePath);
            }
        }

        private string BuildAccountSasToken()
        {
            var policy = new SharedAccessAccountPolicy()
            {
                Permissions = SharedAccessAccountPermissions.Read | SharedAccessAccountPermissions.Write |
                              SharedAccessAccountPermissions.List | SharedAccessAccountPermissions.Create |
                              SharedAccessAccountPermissions.Delete,
                Services = SharedAccessAccountServices.Blob,
                ResourceTypes = SharedAccessAccountResourceTypes.Container | SharedAccessAccountResourceTypes.Object,
                SharedAccessExpiryTime = DateTime.UtcNow.Date.AddYears(1),
                Protocols = SharedAccessProtocol.HttpsOrHttp
            };

            return _storageAccount.GetSharedAccessSignature(policy);
        }


        private MultipartFormDataContent GetFileContent(IFormFile file)
        {
            byte[] data;
            using (var br = new BinaryReader(file.OpenReadStream()))
                data = br.ReadBytes((int) file.OpenReadStream().Length);

            var bytes = new ByteArrayContent(data);

            return new MultipartFormDataContent {{bytes, "file", file.FileName}};
        }

        private async Task<string> CreateLocalFile(IFormFile file, string code)
        {
            var filePath = $@"{code}-{DateTime.Now.Millisecond.ToString()}{Path.GetExtension(file.FileName)}";

            DeleteFile(filePath);

            await using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);

            return filePath;
        }

        private void DeleteFile(string file)
        {
            if (File.Exists(file))
            {
                File.Delete(file);
            }
        }
    }
}