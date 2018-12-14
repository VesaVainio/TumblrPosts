using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;
using QueueInterface.Messages;
using System.Configuration;

namespace QueueInterface
{
    public class QueueAdapter
    {
        CloudQueue photosToDownloadQueue;

        public void Init(TraceWriter log)
        {
            string connectionString = ConfigurationManager.AppSettings["AzureWebJobsStorage"];
            log.Info("PostsTableAdapter/Init got connection string: " + connectionString);
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
