using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;
using QueueInterface.Messages;

namespace QueueInterface
{
    public class QueueAdapter
    {
        CloudQueue photosToDownloadQueue;

        public void Init()
        {
            string connectionString = CloudConfigurationManager.GetSetting("AzureWebJobsStorage");
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);

            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();

            photosToDownloadQueue = queueClient.GetQueueReference("photos-to-download");

            photosToDownloadQueue.CreateIfNotExists();
        }

        public void SendPhotosToDownload(PhotosToDownload photosToDownload)
        {
            string jsonMessage = JsonConvert.SerializeObject(photosToDownload);
            CloudQueueMessage message = new CloudQueueMessage(jsonMessage);
            photosToDownloadQueue.AddMessage(message);
        }
    }
}
