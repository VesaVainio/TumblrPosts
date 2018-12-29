using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using QueueInterface;
using QueueInterface.Messages;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using TableInterface;

namespace Functions
{
    public static class InitIndexing
    {
        [FunctionName("InitIndexing")]
        public static HttpResponseMessage Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "initindexing")]HttpRequestMessage req, TraceWriter log)
        {
            Startup.Init();

            PostsTableAdapter postsTableAdapter = new PostsTableAdapter();
            postsTableAdapter.Init(log);

            BlogToIndexQueueAdapter blogToIndexQueueAdapter = new BlogToIndexQueueAdapter();
            blogToIndexQueueAdapter.Init();

            List<string> partitions = postsTableAdapter.GetAllPartitions();

            foreach (string blogname in partitions)
            {
                blogToIndexQueueAdapter.Send(new BlogToIndex { Blogname = blogname });
            }

            return req.CreateResponse(HttpStatusCode.OK, "Queued " + partitions.Count + " blogs for indexing");
        }
    }
}
