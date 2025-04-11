using Azure.Identity;
using Azure.Storage;
using Azure.Storage.Blobs;
using System.Runtime.CompilerServices;

namespace Congen.Storage.Data
{
    public static class Util
    {
        public static BlobServiceClient BlobClient;

        public static BlobServiceClient Init(string accountName, StorageSharedKeyCredential cred)
        {
            try
            {
                BlobClient = InitBlobServiceClient(accountName, cred);

                return BlobClient;
            }

            catch(Exception ex)
            {
                throw ex;
            }
        }

        private static BlobServiceClient InitBlobServiceClient(string accountUrl, StorageSharedKeyCredential cred)
        {
            try
            {
                var client = new BlobServiceClient(new Uri(accountUrl), cred);

                return client;
            }

            catch(Exception ex)
            {
                throw new Exception("Could not create blob service client.");
            }
        }
    }
}
