using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;
using QueueInterface.Messages;
using System.Configuration;
using TumblrPics.Model;

namespace QueueInterface
{
    public class MediaToDownloadQueueAdapter
    {
        private static readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };

        CloudQueue photosToDownloadQueue;
        CloudQueue videosToDownloadQueue;

        public void Init(TraceWriter log)
        {
            string connectionString = ConfigurationManager.AppSettings["AzureWebJobsStorage"];
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);

            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();

            photosToDownloadQueue = queueClient.GetQueueReference(Constants.PhotosToDownloadQueueName);

            videosToDownloadQueue = queueClient.GetQueueReference(Constants.VideosToDownloadQueueName);
        }

        public void SendPhotosToDownload(PhotosToDownload photosToDownload)
        {
            string jsonMessage = JsonConvert.SerializeObject(photosToDownload, JsonSerializerSettings);
            CloudQueueMessage message = new CloudQueueMessage(jsonMessage);
            photosToDownloadQueue.AddMessage(message);
        }

        public void SendVideosToDownload(VideosToDownload videoToDownload)
        {
            string jsonMessage = JsonConvert.SerializeObject(videoToDownload, JsonSerializerSettings);
            CloudQueueMessage message = new CloudQueueMessage(jsonMessage);
            videosToDownloadQueue.AddMessage(message);
        }
    }
}
