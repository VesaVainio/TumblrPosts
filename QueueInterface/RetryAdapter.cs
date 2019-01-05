using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using System;
using System.Configuration;
using TumblrPics.Model;

namespace QueueInterface
{
    public class RetryAdapter
    {
        public static void RetryPoisonMessages(TraceWriter log)
        {
            string connectionString = ConfigurationManager.AppSettings["AzureWebJobsStorage"];
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);

            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();

            RetryPoisonMesssages(queueClient, Constants.PostsToProcessQueueName, log);
            RetryPoisonMesssages(queueClient, Constants.PhotosToDownloadQueueName, log);
            RetryPoisonMesssages(queueClient, Constants.VideosToDownloadQueueName, log);
            RetryPoisonMesssages(queueClient, Constants.BlogToIndexQueueName, log);
        }

        private static int RetryPoisonMesssages(CloudQueueClient queueClient, string queueName, TraceWriter log)
        {
            CloudQueue targetqueue = queueClient.GetQueueReference(queueName);
            CloudQueue poisonqueue = queueClient.GetQueueReference(queueName + "-poison");

            int count = 0;
            int skippedCount = 0;
            while (true)
            {
                CloudQueueMessage msg = poisonqueue.GetMessage();
                if (msg == null)
                    break;

                DateTime cutoffTime = DateTime.UtcNow.AddMinutes(-5.0);

                if (msg.InsertionTime < cutoffTime)
                {
                    poisonqueue.DeleteMessage(msg);
                    targetqueue.AddMessage(msg);
                    count++;
                } else
                {
                    skippedCount++;
                }
            }

            log.Info("Moved " + count + " messages to queue " + queueName + ", skipped" + skippedCount + " messages");

            return count;
        }

        private static CloudQueue GetCloudQueueRef(CloudQueueClient queueClient, string queuename)
        {
            CloudQueue queue = queueClient.GetQueueReference(queuename);

            return queue;
        }
    }
}
