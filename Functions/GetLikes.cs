using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using QueueInterface;
using System;
using System.Configuration;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using TumblrPics.Model.Tumblr;

namespace Functions
{
    public static class GetLikes
    {
        [FunctionName("GetLikes")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "getlikes/{blogname}")]HttpRequestMessage req,
            string blogname, TraceWriter log)
        {
            Startup.Init();

            PostsToProcessQueueAdapter postsToProcessQueueAdapter = new PostsToProcessQueueAdapter();
            postsToProcessQueueAdapter.Init(log);

            Likes likes = null;
            long totalCount = 0;
            using (HttpClient httpClient = new HttpClient())
            {
                DateTime beforeTime = DateTime.UtcNow;
                do
                {
                    string apiKey = ConfigurationManager.AppSettings["TumblrApiKey"];
                    string url;
                    if (likes == null)
                    {
                        long timestamp = GetUnixTime(beforeTime);
                        url = "https://api.tumblr.com/v2/blog/" + blogname + "/likes?before=" + timestamp + "&api_key=" + apiKey;
                    }
                    else
                    {
                        url = "https://api.tumblr.com/" + likes._links.Next.Href + "&api_key=" + apiKey;
                    }
                    log.Info("Making request to: " + url);
                    HttpResponseMessage response = await httpClient.GetAsync(url);
                    if (response.IsSuccessStatusCode)
                    {
                        TumblrResponse<Likes> tumblrResponse = await response.Content.ReadAsAsync<TumblrResponse<Likes>>();
                        likes = tumblrResponse.Response;

                        totalCount += likes.Liked_posts.Count;

                        postsToProcessQueueAdapter.SendPostsToProcess(likes.Liked_posts, blogname);
                    }
                    else
                    {
                        log.Info("Got response: " + response.ReasonPhrase + " " + response.StatusCode);
                        break;
                    }

                } while (likes._links != null && likes._links.Next != null && !string.IsNullOrEmpty(likes._links.Next.Href));
            }

            log.Info("C# HTTP trigger function processed a request.");

            // Fetching the name from the path parameter in the request URL
            return req.CreateResponse(HttpStatusCode.OK, "Got " + totalCount + " posts");
        }

        private static long GetUnixTime(DateTime dateTime)
        {
            DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            TimeSpan span = (dateTime.ToUniversalTime() - epoch);
            return Convert.ToInt64(span.TotalSeconds);
        }
    }
}
