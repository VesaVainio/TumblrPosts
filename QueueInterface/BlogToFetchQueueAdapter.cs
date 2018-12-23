using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;
using QueueInterface.Messages;
using System;
using System.Configuration;
using System.Threading.Tasks;
using TumblrPics.Model;

namespace QueueInterface
{
    public class BlogToFetchQueueAdapter
    {
        private static readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };

        CloudQueue blogsToFetchQueue;

        public void Init()
        {
            string connectionString = ConfigurationManager.AppSettings["AzureWebJobsStorage"];
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);

            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();

            blogsToFetchQueue = queueClient.GetQueueReference(Constants.BlogToFetchQueueName);
        }


        public void SendBlogToFetch(BlogToFetch blogToFetch)
        {
            string jsonMessage = JsonConvert.SerializeObject(blogToFetch, JsonSerializerSettings);
            CloudQueueMessage message = new CloudQueueMessage(jsonMessage);
            blogsToFetchQueue.AddMessage(message);
        }

        public async Task<CloudQueueMessage> GetNextMessage()
        {
            return await blogsToFetchQueue.GetMessageAsync(TimeSpan.FromMinutes(6), null, null);
        }

        public async Task DeleteMessage(CloudQueueMessage message)
        {
            await blogsToFetchQueue.DeleteMessageAsync(message);
        }
    }
}
