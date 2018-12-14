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
            FunctionUtilities.ConfigureBindingRedirects();
            log.Info("BindingRedirects configured.");

            PostsToProcessQueueAdapter postsToProcessQueueAdapter = new PostsToProcessQueueAdapter();
            postsToProcessQueueAdapter.Init(log);

            //PostProcessor postProcessor = new PostProcessor();
            //postProcessor.Init(log);

            log.Info("PostProcessor initialized.");

            BlogPosts blogPosts = null;
            using (HttpClient httpClient = new HttpClient())
            {
                long totalCount = 0;
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

                        totalCount = blogPosts.Blog.Posts;
                        offset += 20;

                        if (blogPosts.Posts != null && blogPosts.Posts.Count > 0)
                        {
                            postsToProcessQueueAdapter.SendPostsToProcess(blogPosts.Posts);
                        }
                        //postProcessor.ProcessPosts(blogPosts.Posts, log);
                    }

                } while (offset < totalCount);
            }

            log.Info("C# HTTP trigger function processed a request.");

            // Fetching the name from the path parameter in the request URL
            return req.CreateResponse(HttpStatusCode.OK, "Got " + blogPosts.Posts.Count + " posts");
        }
    }
}
