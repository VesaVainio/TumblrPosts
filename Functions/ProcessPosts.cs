using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using QueueInterface.Messages;

namespace Functions
{
    public static class ProcessPosts
    {
        [FunctionName("PostsToProcess")]
        public static void Run([QueueTrigger("posts-to-process", Connection = "AzureWebJobsStorage")]string myQueueItem, TraceWriter log)
        {
            Startup.Init();

            log.Info($"C# Queue trigger function processed: {myQueueItem}");

            PostsToProcess postsToProcess = JsonConvert.DeserializeObject<PostsToProcess>(myQueueItem);

            PostProcessor postProcessor = new PostProcessor();
            postProcessor.Init(log);

            postProcessor.ProcessPosts(postsToProcess.Posts, log, postsToProcess.LikerBlogname);
        }
    }
}
