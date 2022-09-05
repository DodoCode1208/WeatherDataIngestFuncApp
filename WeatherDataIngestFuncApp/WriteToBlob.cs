using Azure.Storage.Blobs;
using System;
using System.Collections.Generic;
using System.Text;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using System.Reflection.Metadata;
using System.IO;
using Microsoft.Extensions.Options;

namespace WeatherDataIngestFuncApp
{
    public class WriteToBlob : IWriteToBlob
    {
        private readonly StorageConfigOptions _options;
        private readonly BlobServiceClient _blobServiceClient;

        //Injecting classes via dependency Injection
        public WriteToBlob( IOptions<StorageConfigOptions> options, BlobServiceClient blobServiceClient)
        {
            _blobServiceClient = blobServiceClient;
            _options = options.Value;
        }
        
        //Write Json to blob storage (azure)
        public  async Task WriteToblob(string content, string blobFileName)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient("weatherdataingestcontainer");
            var blobClient = containerClient.GetBlobClient(blobFileName);

            MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
            var task = await blobClient.UploadAsync(stream);
        }
    }
}
