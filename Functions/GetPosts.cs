using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using QueueInterface;
using System.Configuration;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using TumblrPics.Model.Tumblr;

namespace Functions
{
    public static class GetPosts
    {
        [FunctionName("GetPosts")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "getposts/{blogname}")]HttpRequestMessage req, 
            string blogname, TraceWriter log)
        {
            Startup.Init();
            log.Info("BindingRedirects configured.");

            PostsToProcessQueueAdapter postsToProcessQueueAdapter = new PostsToProcessQueueAdapter();
            postsToProcessQueueAdapter.Init(log);

            log.Info("PostProcessor initialized.");

            long totalInBlog = 0;
            long totalReceived = 0;
            BlogPosts blogPosts = null;
            using (HttpClient httpClient = new HttpClient())
            {
                
                int offset = 0;
                string apiKey = ConfigurationManager.AppSettings["TumblrApiKey"];
                do
                {
                    string url = "https://api.tumblr.com/v2/blog/" + blogname + "/posts?offset=" + offset + "&api_key=" + apiKey;
                    log.Info("Making request to: " + url);
                    HttpResponseMessage response = await httpClient.GetAsync(url);
                    if (response.IsSuccessStatusCode)
                    {
                        TumblrResponse<BlogPosts> tumblrResponse = await response.Content.ReadAsAsync<TumblrResponse<BlogPosts>>();
                        blogPosts = tumblrResponse.Response;

                        totalInBlog = blogPosts.Blog.Posts;
                        totalReceived += blogPosts.Posts.Count;
                        offset += 20;

                        if (blogPosts.Posts != null && blogPosts.Posts.Count > 0)
                        {
                            postsToProcessQueueAdapter.SendPostsToProcess(blogPosts.Posts);
                        }
                    }
                    else
                    {
                        return req.CreateResponse(response.StatusCode, "Getting posts got an error: " + response.ReasonPhrase);
                    }

                } while (offset < totalInBlog);
            }

            log.Info("C# HTTP trigger function processed a request.");

            // Fetching the name from the path parameter in the request URL
            return req.CreateResponse(HttpStatusCode.OK, "Queued  " + totalReceived + "/" + totalInBlog + " posts for processing");
        }
    }
}
