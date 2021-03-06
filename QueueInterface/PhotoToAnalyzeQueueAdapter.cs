﻿using System;
using System.Configuration;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;
using QueueInterface.Messages;
using TumblrPics.Model;

namespace QueueInterface
{
    public class PhotoToAnalyzeQueueAdapter
    {
        private CloudQueue photoToAnalyzeQueue;
        private CloudQueue photoToAnalyzePoisonQueue;

        public void Init()
        {
            string connectionString = ConfigurationManager.AppSettings["AzureWebJobsStorage"];
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);

            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();

            photoToAnalyzeQueue = queueClient.GetQueueReference(Constants.PhotoToAnalyzeQueueName);
            photoToAnalyzePoisonQueue = queueClient.GetQueueReference(Constants.PhotoToAnalyzeQueueName + "-poison");
        }

        public void Send(PhotoToAnalyze photoToAnalyze)
        {
            string jsonMessage = JsonConvert.SerializeObject(photoToAnalyze);
            CloudQueueMessage message = new CloudQueueMessage(jsonMessage);
            photoToAnalyzeQueue.AddMessage(message);
        }

        public async Task SendToPoisonQueue(PhotoToAnalyze photoToAnalyze)
        {
            string jsonMessage = JsonConvert.SerializeObject(photoToAnalyze);
            CloudQueueMessage message = new CloudQueueMessage(jsonMessage);
            await photoToAnalyzePoisonQueue.AddMessageAsync(message);
        }

        public async Task<CloudQueueMessage> GetNextMessage()
        {
            return await photoToAnalyzeQueue.GetMessageAsync(TimeSpan.FromMinutes(6), null, null);
        }

        public async Task DeleteMessage(CloudQueueMessage message)
        {
            await photoToAnalyzeQueue.DeleteMessageAsync(message);
        }
    }
}