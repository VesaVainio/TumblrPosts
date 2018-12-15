using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using System.Configuration;
using TumblrPics.Model;

namespace QueueInterface
{
    public static class QueueInterfaceStartup
    {
        public static void Init()
        {
            string connectionString = ConfigurationManager.AppSettings["AzureWebJobsStorage"];
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);

            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();

            CloudQueue postsToProcessQueue = queueClient.GetQueueReference("posts-to-process");
            postsToProcessQueue.CreateIfNotExists();

            CloudQueue photosToDownloadQueue = queueClient.GetQueueReference(Constants.PhotosToDownloadQueueName);
            photosToDownloadQueue.CreateIfNotExists();

            CloudQueue videosToDownloadQueue = queueClient.GetQueueReference(Constants.VideosToDownloadQueueName);
            videosToDownloadQueue.CreateIfNotExists();
        }
    }
}
