using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;
using QueueInterface;
using QueueInterface.Messages;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using TableInterface;
using TableInterface.Entities;

namespace Functions
{
    public static class ProcessBlogToFetch
    {
        [FunctionName("ProcessBlogToFetch")]
        public static async Task Run([TimerTrigger("0 30 */2 * * *")]TimerInfo myTimer, TraceWriter log)
        {
            log.Info($"C# Timer trigger function executed at: {DateTime.Now}");

            BlogToFetchQueueAdapter blogToFetchQueueAdapter = new BlogToFetchQueueAdapter();
            blogToFetchQueueAdapter.Init();

            BlogInfoTableAdapter blogInfoTableAdapter = new BlogInfoTableAdapter();
            blogInfoTableAdapter.Init();

            PostsGetter postsGetter = new PostsGetter();
            GetPostsResult result = null;

            Stopwatch stopwatch = Stopwatch.StartNew();

            do
            {
                CloudQueueMessage message = await blogToFetchQueueAdapter.GetNextMessage();
                if (message == null)
                {
                    return;
                }

                BlogToFetch blogToFetch = JsonConvert.DeserializeObject<BlogToFetch>(message.AsString);

                BlogEntity blogEntity = await blogInfoTableAdapter.GetBlog(blogToFetch.Blogname);

                int offset = 0;
                if (blogEntity != null && blogEntity.FetchedUntilOffset.HasValue)
                {
                    offset = blogEntity.FetchedUntilOffset.Value;
                }

                long timeoutLeft = 270 - (stopwatch.ElapsedMilliseconds / 1000);
                if (timeoutLeft < 10)
                {
                    return;
                }

                result = await postsGetter.GetPosts(log, blogToFetch.Blogname, offset, timeoutSeconds: timeoutLeft);
                if (result.Success)
                {
                    await blogToFetchQueueAdapter.DeleteMessage(message);
                }
            } while (result != null && result.Success);
        }
    }
}
