using System.Configuration;
using System.Linq;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Model.Tumblr;
using Newtonsoft.Json;
using QueueInterface.Messages;
using TumblrPics.Model;

namespace QueueInterface
{
    public class MediaToDownloadQueueAdapter
    {
        private static readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };
        private TraceWriter log;

        private CloudQueue photosToDownloadQueue;
        private CloudQueue videosToDownloadQueue;

        public void Init(TraceWriter traceWriter)
        {
            log = traceWriter;
            string connectionString = ConfigurationManager.AppSettings["AzureWebJobsStorage"];
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);

            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();

            photosToDownloadQueue = queueClient.GetQueueReference(Constants.PhotosToDownloadQueueName);

            videosToDownloadQueue = queueClient.GetQueueReference(Constants.VideosToDownloadQueueName);
        }

        public bool SendPhotosToDownload(PhotosToDownload photosToDownload, bool terminateRecursion = false)
        {
            string jsonMessage = JsonConvert.SerializeObject(photosToDownload, JsonSerializerSettings);

            if (jsonMessage.Length > 45000)
            {
                if (photosToDownload.Photos.Length > 1)
                {
                    int half = photosToDownload.Photos.Length / 2;

                    Photo[] photos1 = photosToDownload.Photos.Take(half).ToArray();
                    Photo[] photos2 = photosToDownload.Photos.Skip(half).Take(photosToDownload.Photos.Length - half).ToArray();

                    photosToDownload.Photos = photos1;
                    SendPhotosToDownload(photosToDownload);

                    photosToDownload.Photos = photos2;
                    SendPhotosToDownload(photosToDownload);

                    return true;
                }

                if (!terminateRecursion)
                {
                    bool result = SendPhotosToDownload(photosToDownload, true);
                    if (!result)
                    {
                        log.Error("Single post too long (" + jsonMessage.Length + " chars)");
                    }

                    return result;
                }

                return false;
            }

            CloudQueueMessage message = new CloudQueueMessage(jsonMessage);
            photosToDownloadQueue.AddMessage(message);
            return true;
        }

        public void SendVideosToDownload(VideosToDownload videoToDownload)
        {
            string jsonMessage = JsonConvert.SerializeObject(videoToDownload, JsonSerializerSettings);
            CloudQueueMessage message = new CloudQueueMessage(jsonMessage);
            videosToDownloadQueue.AddMessage(message);
        }
    }
}