﻿using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;
using QueueInterface.Messages;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using TumblrPics.Model.Tumblr;

namespace QueueInterface
{
    public class PostsToProcessQueueAdapter
    {
        private static readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };

        private CloudQueue postsToProcessQueue;
        private TraceWriter log;

        public void Init(TraceWriter log)
        {
            this.log = log;
            string connectionString = ConfigurationManager.AppSettings["AzureWebJobsStorage"];
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);

            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();

            postsToProcessQueue = queueClient.GetQueueReference("posts-to-process");
        }

        public bool SendPostsToProcess(IEnumerable<Post> posts, string likerBlogName = null, bool terminateRecursion = false)
        {
            Post[] postsArray = posts.ToArray();

            PostsToProcess postsToProcess = new PostsToProcess
            {
                Posts = postsArray,
                LikerBlogname = likerBlogName
            };
            string jsonMessage = JsonConvert.SerializeObject(postsToProcess, JsonSerializerSettings);

            if (jsonMessage.Length > 45000)
            {
                if (postsArray.Length > 1)
                {
                    int half = postsArray.Length / 2;

                    Post[] posts1 = posts.Take(half).ToArray();
                    SendPostsToProcess(posts1, likerBlogName);

                    Post[] posts2 = posts.Skip(half).Take(postsArray.Length - half).ToArray();
                    SendPostsToProcess(posts2, likerBlogName);

                    return true;
                }
                else if (!terminateRecursion)
                {
                    bool result = SendPostsToProcess(postsArray, likerBlogName, true);
                    if (!result)
                    {
                        log.Error("Single post too long (" + jsonMessage.Length + " chars)");
                    }
                    return result;
                }
                else
                {
                    return false;
                }
            }

            CloudQueueMessage message = new CloudQueueMessage(jsonMessage);
            postsToProcessQueue.AddMessage(message);
            return true;
        }
    }
}