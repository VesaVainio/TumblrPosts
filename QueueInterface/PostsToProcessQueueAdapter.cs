using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;
using QueueInterface.Messages;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using Model.Tumblr;
using TumblrPics.Model;

namespace QueueInterface
{
    public class PostsToProcessQueueAdapter
    {
        private static readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };

        private CloudQueue postsToProcessQueue;
        private TraceWriter log;

        public void Init(TraceWriter traceWriter)
        {
            log = traceWriter;
            string connectionString = ConfigurationManager.AppSettings["AzureWebJobsStorage"];
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);

            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();

            postsToProcessQueue = queueClient.GetQueueReference(Constants.PostsToProcessQueueName);
        }

        public bool SendPostsToProcess(IEnumerable<Post> posts, string likerBlogName = null, bool terminateRecursion = false)
        {
            Post[] postsArray = posts.ToArray();

            StripSmallPlayers(postsArray); // minimize the size by including only the largest player

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

                    Post[] posts1 = postsArray.Take(half).ToArray();
                    SendPostsToProcess(posts1, likerBlogName);

                    Post[] posts2 = postsArray.Skip(half).Take(postsArray.Length - half).ToArray();
                    SendPostsToProcess(posts2, likerBlogName);

                    return true;
                }

                if (!terminateRecursion)
                {
                    bool result = SendPostsToProcess(postsArray, likerBlogName, true);
                    if (!result)
                    {
                        log.Error("Single post too long (" + jsonMessage.Length + " chars)");
                    }
                    return result;
                }

                return false;
            }

            CloudQueueMessage message = new CloudQueueMessage(jsonMessage);
            postsToProcessQueue.AddMessage(message);
            return true;
        }

        public void StripSmallPlayers(Post[] posts)
        {
            foreach (Post post in posts)
            {
                if (post.Player != null && post.Player.Length > 0)
                {
                    Player largestPlayer = post.Player.OrderBy(x => x.Width).Last();
                    post.Player = new Player[] { largestPlayer };
                }
            }
        }
    }
}
