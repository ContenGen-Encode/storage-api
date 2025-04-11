using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Congen.Storage.Data;

namespace Congen.Storage.Business
{
    public class StorageRepo : IStorageRepo
    {
        public string SaveFile(Stream file, string extension)
        {
            string fileName = string.Empty;

            try
            {
                var blob = Util.BlobClient.GetBlobContainerClient("contentgen-storage1");
                blob.CreateIfNotExists();

                var guid = Guid.NewGuid().ToString();

                fileName = $"{DateTime.UtcNow.ToString("yy-MM-dd.hh:mm:ss")}-{guid}.{extension}";

                blob.UploadBlob(fileName, file);
            }

            catch(Exception ex)
            {
                throw new Exception("Could not upload file to blob storage.", ex);
            }

            return fileName;
        }
    }
}