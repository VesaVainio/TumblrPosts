using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using QueueInterface.Messages;
using System.Threading.Tasks;
using TumblrPics.Model;

namespace Functions
{
    public static class ProcessPosts
    {
        [FunctionName("PostsToProcess")]
        public static async Task Run([QueueTrigger(Constants.PostsToProcessQueueName, Connection = "AzureWebJobsStorage")]string myQueueItem, TraceWriter log)
        {
            Startup.Init();

            PostsToProcess postsToProcess = JsonConvert.DeserializeObject<PostsToProcess>(myQueueItem);

            PostProcessor postProcessor = new PostProcessor();
            postProcessor.Init(log);

            await postProcessor.ProcessPosts(postsToProcess.Posts, log, postsToProcess.LikerBlogname);
        }
    }
}
