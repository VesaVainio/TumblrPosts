using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;
using QueueInterface.Messages;
using System.Configuration;

namespace QueueInterface
{
    public class MediaToDownloadQueueAdapter
    {
        private static readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };

        CloudQueue photosToDownloadQueue;
        CloudQueue videoToDownloadQueue;

        public void Init(TraceWriter log)
        {
            string connectionString = ConfigurationManager.AppSettings["AzureWebJobsStorage"];
            //log.Info("MediaToDownloadQueueAdapter/Init got connection string: " + connectionString);
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);

            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();

            photosToDownloadQueue = queueClient.GetQueueReference("photos-to-download");
            photosToDownloadQueue.CreateIfNotExists();

            videoToDownloadQueue = queueClient.GetQueueReference("video-to-download");
            videoToDownloadQueue.CreateIfNotExists();
        }

        public void SendPhotosToDownload(PhotosToDownload photosToDownload)
        {
            string jsonMessage = JsonConvert.SerializeObject(photosToDownload, JsonSerializerSettings);
            CloudQueueMessage message = new CloudQueueMessage(jsonMessage);
            photosToDownloadQueue.AddMessage(message);
        }

        public void SendVideoToDownload(VideoToDownload videoToDownload)
        {
            string jsonMessage = JsonConvert.SerializeObject(videoToDownload, JsonSerializerSettings);
            CloudQueueMessage message = new CloudQueueMessage(jsonMessage);
            videoToDownloadQueue.AddMessage(message);
        }
    }
}
