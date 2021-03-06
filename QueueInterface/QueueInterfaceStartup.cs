﻿using Microsoft.WindowsAzure.Storage;
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

            CloudQueue postsToProcessQueue = queueClient.GetQueueReference(Constants.PostsToProcessQueueName);
            postsToProcessQueue.CreateIfNotExists();

            CloudQueue photosToDownloadQueue = queueClient.GetQueueReference(Constants.PhotosToDownloadQueueName);
            photosToDownloadQueue.CreateIfNotExists();

            CloudQueue videosToDownloadQueue = queueClient.GetQueueReference(Constants.VideosToDownloadQueueName);
            videosToDownloadQueue.CreateIfNotExists();

            CloudQueue blogToFetchQueue = queueClient.GetQueueReference(Constants.BlogToFetchQueueName);
            blogToFetchQueue.CreateIfNotExists();

            CloudQueue postToGetQueue = queueClient.GetQueueReference(Constants.PostToGetQueueName);
            postToGetQueue.CreateIfNotExists();

            CloudQueue blogToIndexQueue = queueClient.GetQueueReference(Constants.BlogToIndexQueueName);
            blogToIndexQueue.CreateIfNotExists();

            CloudQueue photoToAnalyzeQueue = queueClient.GetQueueReference(Constants.PhotoToAnalyzeQueueName);
            photoToAnalyzeQueue.CreateIfNotExists();

            CloudQueue photoToAnalyzePoisonQueue = queueClient.GetQueueReference(Constants.PhotoToAnalyzeQueueName + "-poison");
            photoToAnalyzePoisonQueue.CreateIfNotExists();
        }
    }
}
