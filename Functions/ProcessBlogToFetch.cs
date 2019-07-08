using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;
using QueueInterface;
using QueueInterface.Messages;
using TableInterface;
using TableInterface.Entities;
using TumblrPics.Model;

namespace Functions
{
    public static class ProcessBlogToFetch
    {
        [FunctionName("ProcessBlogToFetch")]
        public static async Task Run([TimerTrigger("0 09 * * * *")] TimerInfo myTimer, TraceWriter log)
        {
            Startup.Init();

            log.Info($"C# Timer trigger function executed at: {DateTime.Now}");

            BlogToFetchQueueAdapter blogToFetchQueueAdapter = new BlogToFetchQueueAdapter();
            blogToFetchQueueAdapter.Init();

            BlogInfoTableAdapter blogInfoTableAdapter = new BlogInfoTableAdapter();
            blogInfoTableAdapter.Init();

            PostsGetter postsGetter = new PostsGetter();

            Stopwatch stopwatch = Stopwatch.StartNew();

            bool success = false; // basically means that can the message be deleted, or if it needs to be left in queue to be resumed later

            do
            {
                CloudQueueMessage message = await blogToFetchQueueAdapter.GetNextMessage();
                if (message == null)
                {
                    return;
                }

                BlogToFetch blogToFetch = JsonConvert.DeserializeObject<BlogToFetch>(message.AsString);

                BlogEntity blogEntity = await blogInfoTableAdapter.GetBlog(blogToFetch.Blogname);

                long timeoutLeft = 270 - stopwatch.ElapsedMilliseconds / 1000;
                if (timeoutLeft < 10)
                {
                    return;
                }

                success = false;

                GetPostsResult result;
                if (blogToFetch.NewerThan.HasValue)
                {
                    result = await postsGetter.GetNewerPosts(log, blogToFetch.Blogname, blogToFetch.NewerThan.Value, timeoutLeft);
                    if (result.Success)
                    {
                        success = true;
                    }
                }

                int offset = 0;
                if (blogEntity != null && blogEntity.FetchedUntilOffset.HasValue)
                {
                    offset = blogEntity.FetchedUntilOffset.Value;
                }

                if (!blogEntity.FetchedUntilOffset.HasValue || blogEntity.FetchedUntilOffset.Value < Constants.MaxPostsToFetch)
                {
                    result = await postsGetter.GetPosts(log, blogToFetch.Blogname, offset, timeoutSeconds: timeoutLeft);
                    if (result.Success)
                    {
                        success = true;
                    }
                }
                else
                {
                    success = true; // enough fetched already, message can be deleted
                }

                if (success)
                {
                    await blogToFetchQueueAdapter.DeleteMessage(message);
                }
            } while (success);
        }
    }
}