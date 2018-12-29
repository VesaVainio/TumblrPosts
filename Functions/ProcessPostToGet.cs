using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using QueueInterface;
using QueueInterface.Messages;
using System.Configuration;
using System.Net.Http;
using System.Threading.Tasks;
using TumblrPics.Model;
using TumblrPics.Model.Tumblr;

namespace Functions
{
    public static class ProcessPostToGet
    {
        [FunctionName("ProcessPostToGet")]
        public static async Task Run([QueueTrigger(Constants.PostToGetQueueName, Connection = "AzureWebJobsStorage")]string myQueueItem, TraceWriter log)
        {
            PostToGet postToGet = JsonConvert.DeserializeObject<PostToGet>(myQueueItem);

            PostsToProcessQueueAdapter postsToProcessQueueAdapter = new PostsToProcessQueueAdapter();
            postsToProcessQueueAdapter.Init(log);

            using (HttpClient httpClient = new HttpClient())
            {
                string apiKey = ConfigurationManager.AppSettings["TumblrApiKey"];

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
                }
                else
                {
                    log.Error("Error getting post " + postToGet.Blogname + "/" + postToGet.Id + ": " + response.ReasonPhrase);
                }
            }
        }
    }
}
