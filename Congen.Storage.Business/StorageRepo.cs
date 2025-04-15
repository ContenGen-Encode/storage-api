using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Congen.Storage.Data;

namespace Congen.Storage.Business
{
    public class StorageRepo : IStorageRepo
    {
        public string SaveFile(string container, Stream file, string extension)
        {
            string fileName = string.Empty;

            try
            {
                var blob = Util.BlobClient.GetBlobContainerClient(container);
                blob.CreateIfNotExists();
                blob.SetAccessPolicy(PublicAccessType.Blob);

                var guid = Guid.NewGuid().ToString();

                fileName = $"{guid}.{extension}";

                blob.UploadBlob(fileName, file);

                var blobClient = blob.GetBlobClient(fileName);

                fileName = blobClient.Uri.ToString();

                //blobClient.SetAccessTier(AccessTier.p)`
            }

            catch(Exception ex)
            {
                throw new Exception("Could not upload file to blob storage.", ex);
            }

            return fileName;
        }

        public Stream[] GetFiles(string container, string[] fileNames)
        {
            Stream[] streams = new Stream[fileNames.Length];

            try
            {
                var blob = Util.BlobClient.GetBlobContainerClient(container);
                blob.CreateIfNotExists();
                foreach(var fileName in fileNames)
                {
                    streams[Array.IndexOf(fileNames, fileName)] = GetFile(blob.GetBlobClient(container), fileName);
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Could not download file from blob storage. Perhaps it is not there?");
            }

            return streams;
        }

        public Stream GetFile(string container, string fileName)
        {
            Stream stream = new MemoryStream();
            try
            {
                var blob = Util.BlobClient.GetBlobContainerClient(container);
                blob.CreateIfNotExists();
                var blobClient = blob.GetBlobClient(fileName);

                Response<BlobDownloadInfo> downloadInfo = blobClient.Download();
                downloadInfo.Value.Content.CopyTo(stream);

                stream.Position = 0;
            }

            catch (Exception ex)
            {
                throw new Exception("Could not download file from blob storage. Perhaps it is not there?");
            }

            return stream;
        }

        public Stream GetFile(BlobClient blobClient, string fileName)
        {
            Stream stream = new MemoryStream();
            try
            {
                Response<BlobDownloadInfo> downloadInfo = blobClient.Download();
                downloadInfo.Value.Content.CopyTo(stream);

                stream.Position = 0;
            }

            catch (Exception ex)
            {
                throw new Exception("Could not download file from blob storage. Perhaps it is not there?");
            }
            return stream;
        }
    }
}