using Microsoft.Azure.WebJobs.Host;
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

        public void Init(TraceWriter log)
        {
            string connectionString = ConfigurationManager.AppSettings["AzureWebJobsStorage"];
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);

            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();

            postsToProcessQueue = queueClient.GetQueueReference("posts-to-process");
        }

        public void SendPostsToProcess(IEnumerable<Post> posts, string likerBlogName = null)
        {
            PostsToProcess postsToProcess = new PostsToProcess
            {
                Posts = posts.ToArray(),
                LikerBlogname = likerBlogName
            };
            string jsonMessage = JsonConvert.SerializeObject(postsToProcess, JsonSerializerSettings);

            if (jsonMessage.Length > 45000)
            {
                Post[] postsArray = posts.ToArray();
                int half = postsArray.Length / 2;

                Post[] posts1 = posts.Take(half).ToArray();
                SendPostsToProcess(posts1, likerBlogName);

                Post[] posts2 = posts.Skip(half).Take(postsArray.Length - half).ToArray();
                SendPostsToProcess(posts2, likerBlogName);

                return;
            }

            CloudQueueMessage message = new CloudQueueMessage(jsonMessage);
            postsToProcessQueue.AddMessage(message);
        }
    }
}
