using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using QueueInterface;
using TableInterface;
using TumblrPics.Model.Tumblr;

namespace Functions
{
    public static class GetLikes
    {
        [FunctionName("GetLikes")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "getlikes/{blogname}")]
            HttpRequestMessage req,
            string blogname, TraceWriter log)
        {
            Startup.Init();

            blogname = blogname.ToLower().Replace(".tumblr.com", "");

            PostsToProcessQueueAdapter postsToProcessQueueAdapter = new PostsToProcessQueueAdapter();
            postsToProcessQueueAdapter.Init(log);

            LikeIndexTableAdapter likeIndexTableAdapter = new LikeIndexTableAdapter();
            likeIndexTableAdapter.Init();

            long newestLikedTimestamp = likeIndexTableAdapter.GetNewestLikedTimestamp(blogname);

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
                        long timestamp = FunctionUtilities.GetUnixTime(beforeTime);
                        url = "https://api.tumblr.com/v2/blog/" + blogname + "/likes?before=" + timestamp + "&api_key=" + apiKey;
                    }
                    else
                    {
                        url = "https://api.tumblr.com" + likes._links.Next.Href + "&api_key=" + apiKey;
                    }

                    log.Info("Making request to: " + url);
                    HttpResponseMessage response = await httpClient.GetAsync(url);
                    if (response.IsSuccessStatusCode)
                    {
                        TumblrResponse<Likes> tumblrResponse = await response.Content.ReadAsAsync<TumblrResponse<Likes>>();
                        likes = tumblrResponse.Response;

                        if (newestLikedTimestamp > 0)
                        {
                            List<Post> newerPosts = likes.Liked_posts.Where(x => x.Liked_Timestamp > newestLikedTimestamp).ToList();
                            if (newerPosts.Count < likes.Liked_posts.Count)
                            {
                                log.Info($"Reached Liked_Timestamp of {newestLikedTimestamp} which has already been fetched, finishing");
                                postsToProcessQueueAdapter.SendPostsToProcess(newerPosts, blogname);

                                totalCount += newerPosts.Count;
                                break;
                            }
                        }

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
    }
}