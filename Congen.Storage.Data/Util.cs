using Azure.Core;
using Azure.Identity;
using Azure.Storage;
using Azure.Storage.Blobs;
using Clerk.Net;
using Clerk.Net.Client;
using Clerk.Net.Client.Models;
using Congen.Storage.Data.Data_Objects;
using Newtonsoft.Json;
using RabbitMQ.Client;
using System.Collections.Specialized;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using User = Clerk.Net.Client.Models.User;

namespace Congen.Storage.Data
{
    public static class Util
    {
        public static BlobServiceClient BlobClient;

        private static IConnection connection;
        private static IChannel channel;

        private static ClerkApiClient clerkClient;

        private static string RabbitHost = "congen-api.ofneill.com";
        private static string Exchange = "ai";

        private static string ClerkSecretKey = "";

        public static string AIUrl = "";

        #region Setup

        public static void SetAIUrl(string url)
        {
            AIUrl = url;
        }

        public static void ClerkInit(string secret)
        {
            clerkClient = ClerkApiClientFactory.Create(secret);
        }

        #endregion

        #region Clerk

        public static async Task<User> GetUser(string userId)
        {
            try
            {
                var users = await clerkClient.Users.GetAsync(
                requestConfiguration =>
                {
                    requestConfiguration.QueryParameters.UserId = [userId];
                });

                User? user = users.FirstOrDefault();

                if (user == null)
                {
                    throw new Exception("Failed to retrieve user from Clerk. Perhaps your token is expired?");
                }

                return user;
            }

            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static async Task<string> GetUserContainer(string auth)
        {
            string container = "";
            string userId = "";

            try
            {
                using JsonDocument doc = JsonDocument.Parse(DecodeBase64Url(auth.Split(".")[1]));
                JsonElement root = doc.RootElement;

                if (root.TryGetProperty("sub", out JsonElement subElement))
                {
                    userId = subElement.GetString();
                }

                var user = await GetUser(userId);
                User_private_metadata metadata = user.PrivateMetadata;

                metadata.AdditionalData.TryGetValue("container", out object conainerObj);

                container = conainerObj.ToString();
            }

            catch (Exception ex)
            {
                throw ex;
            }

            return container;
        }

        #endregion

        #region Blob

        public static BlobServiceClient InitBlobStorageClient(string accountName, StorageSharedKeyCredential cred)
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

        #endregion

        #region Insta



        #endregion

        #region Rabbit

        public static async Task InitRabbit(string user, string pass)
        {
            ConnectionFactory factory = new ConnectionFactory()
            {
                HostName = RabbitHost,
                UserName = user,
                Password = pass
            };

            connection = await factory.CreateConnectionAsync();

            //create exchange
            channel = await connection.CreateChannelAsync();

            channel.ExchangeDeclareAsync(Exchange, "topic", true);
        }

        public static async Task SendMessage(string message, string routingKey)
        {
            try
            {
                byte[] body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new { a = "" }));

                await channel.BasicPublishAsync(Exchange, routingKey, body);
            }

            catch (Exception ex)
            {
                throw ex;
            }
        }

        #endregion

        static string DecodeBase64Url(string base64Url)
        {
            string base64 = base64Url.Replace('-', '+').Replace('_', '/');
            switch (base64.Length % 4)
            {
                case 2: base64 += "=="; break;
                case 3: base64 += "="; break;
            }

            byte[] bytes = Convert.FromBase64String(base64);
            return Encoding.UTF8.GetString(bytes);
        }
    }
}
