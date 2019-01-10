using System.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;
using QueueInterface.Messages;

namespace QueueInterface
{
    public class BlobsToFixQueueAdapter
    {
        private static readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings {NullValueHandling = NullValueHandling.Ignore};

        private CloudQueue blobsToFixQueue;

        public void Init()
        {
            string connectionString = ConfigurationManager.AppSettings["AzureWebJobsStorage"];
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);

            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();

            blobsToFixQueue = queueClient.GetQueueReference("blobs-to-fix");
            blobsToFixQueue.CreateIfNotExists();
        }


        public void SendBlobsToFix(BlobsToFix blobsToFix)
        {
            string jsonMessage = JsonConvert.SerializeObject(blobsToFix, JsonSerializerSettings);
            CloudQueueMessage message = new CloudQueueMessage(jsonMessage);
            blobsToFixQueue.AddMessage(message);
        }
    }
}