using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;
using QueueInterface;
using QueueInterface.Messages;
using System;
using System.Configuration;
using System.Net.Http;
using System.Threading.Tasks;
using TableInterface;
using TumblrPics.Model.Tumblr;

namespace Functions
{
    public static class ProcessPostToGet
    {
        [FunctionName("ProcessPostToGet")]
        public static async Task Run([TimerTrigger("0 30 */2 * * *")]TimerInfo myTimer, TraceWriter log)
        {
            PostsToProcessQueueAdapter postsToProcessQueueAdapter = new PostsToProcessQueueAdapter();
            postsToProcessQueueAdapter.Init(log);

            PostToGetQueueAdapter postToGetQueueAdapter = new PostToGetQueueAdapter();
            postToGetQueueAdapter.Init();

            PostsTableAdapter postsTableAdapter = new PostsTableAdapter();
            postsTableAdapter.Init(log);

            using (HttpClient httpClient = new HttpClient())
            {
                string apiKey = ConfigurationManager.AppSettings["TumblrApiKey"];

                do
                {
                    CloudQueueMessage message = await postToGetQueueAdapter.GetNextMessage();
                    if (message == null)
                    {
                        return;
                    }

                    PostToGet postToGet = JsonConvert.DeserializeObject<PostToGet>(message.AsString);
                    string url = "https://api.tumblr.com/v2/blog/" + postToGet.Blogname + "/posts?id=" + postToGet.Id + "&api_key=" + apiKey;
                    log.Info("Making request to: " + url);
                    HttpResponseMessage response = await httpClient.GetAsync(url);
                    if (response.IsSuccessStatusCode)
                    {
                        TumblrResponse<BlogPosts> tumblrResponse = await response.Content.ReadAsAsync<TumblrResponse<BlogPosts>>();
                        BlogPosts blogPosts = tumblrResponse.Response;

                        if (blogPosts.Posts != null && blogPosts.Posts.Count > 0)
                        {
                            postsToProcessQueueAdapter.SendPostsToProcess(blogPosts.Posts);
                        }
                        await postToGetQueueAdapter.DeleteMessage(message);
                        log.Info("Successfully fetched " + postToGet.Blogname + "/" + postToGet.Id + " and queued for processing");
                    }
                    else
                    {
                        log.Error("Error getting post " + postToGet.Blogname + "/" + postToGet.Id + ": " + response.ReasonPhrase);
                        if (response.ReasonPhrase.IndexOf("limit exceeded", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            log.Error("Limit exceeded, exiting");
                            return;
                        }
                        else if (response.ReasonPhrase.IndexOf("not found", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            log.Error("Not found, deleting message, marking post as Not Found");
                            postsTableAdapter.MarkPostNotFound(postToGet.Blogname, postToGet.Id);
                            await postToGetQueueAdapter.DeleteMessage(message);
                        }
                    }
                } while (true);
            }
        }
    }
}
