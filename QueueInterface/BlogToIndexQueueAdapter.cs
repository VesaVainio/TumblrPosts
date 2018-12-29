using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;
using QueueInterface.Messages;
using System.Configuration;
using TumblrPics.Model;

namespace QueueInterface
{
    public class BlogToIndexQueueAdapter
    {
        CloudQueue blogToIndexQueue;

        public void Init()
        {
            string connectionString = ConfigurationManager.AppSettings["AzureWebJobsStorage"];
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);

            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();

            blogToIndexQueue = queueClient.GetQueueReference(Constants.BlogToIndexQueueName);
        }

        public void Send(BlogToIndex blogToIndex)
        {
            string jsonMessage = JsonConvert.SerializeObject(blogToIndex);
            CloudQueueMessage message = new CloudQueueMessage(jsonMessage);
            blogToIndexQueue.AddMessage(message);
        }
    }
}
