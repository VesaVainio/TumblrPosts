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
    public class PostToGetQueueAdapter
    {
        CloudQueue postToGetQueue;

        public void Init()
        {
            string connectionString = ConfigurationManager.AppSettings["AzureWebJobsStorage"];
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);

            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();

            postToGetQueue = queueClient.GetQueueReference(Constants.PostToGetQueueName);
        }

        public void Send(PostToGet postToGet)
        {
            string jsonMessage = JsonConvert.SerializeObject(postToGet);
            CloudQueueMessage message = new CloudQueueMessage(jsonMessage);
            postToGetQueue.AddMessage(message);
        }

        public async Task<CloudQueueMessage> GetNextMessage()
        {
            return await postToGetQueue.GetMessageAsync(TimeSpan.FromMinutes(6), null, null);
        }

        public async Task DeleteMessage(CloudQueueMessage message)
        {
            await postToGetQueue.DeleteMessageAsync(message);
        }
    }
}
