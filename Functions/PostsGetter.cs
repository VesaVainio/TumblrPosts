using Microsoft.Azure.WebJobs.Host;
using QueueInterface;
using System;
using System.Configuration;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using TableInterface;
using TableInterface.Entities;
using TumblrPics.Model.Tumblr;

namespace Functions
{
    public class PostsGetter
    {
        public async Task<GetPostsResult> GetPosts(TraceWriter log, string blogname, int startingOffset = 0, int maxOffset = 3000, long timeoutSeconds = 270)
        {
            PostsToProcessQueueAdapter postsToProcessQueueAdapter = new PostsToProcessQueueAdapter();
            postsToProcessQueueAdapter.Init(log);

            BlogInfoTableAdapter blogInfoTableAdapter = new BlogInfoTableAdapter();
            blogInfoTableAdapter.Init();

            long totalInBlog = 0;
            long totalReceived = 0;
            BlogPosts blogPosts = null;
            Blog blog = null;
            bool success = true;
            int offset = startingOffset;

            using (HttpClient httpClient = new HttpClient())
            {
                Stopwatch stopwatch = Stopwatch.StartNew();

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
                        blog = blogPosts.Blog;
                        totalReceived += blogPosts.Posts.Count;
                        offset += 20;

                        if (blogPosts.Posts != null && blogPosts.Posts.Count > 0)
                        {
                            postsToProcessQueueAdapter.SendPostsToProcess(blogPosts.Posts);
                        }
                    }
                    else
                    {
                        success = false;
                        break;
                    }

                    if (stopwatch.ElapsedMilliseconds > timeoutSeconds * 1000)
                    {
                        success = false;
                        break;
                    }
                } while (offset < totalInBlog && offset < maxOffset);
            }

            BlogEntity blogEntity = new BlogEntity(blog);
            blogEntity.FetchedUntilOffset = offset;
            blogEntity.LastFetched = FunctionUtilities.GetUnixTime(DateTime.UtcNow);
            blogInfoTableAdapter.InsertBlog(blogEntity);

            return new GetPostsResult
            {
                TotalInBlog = totalInBlog,
                TotalReceived = totalReceived,
                Success = success
            };
        }
    }

    public class GetPostsResult
    {
        public long TotalInBlog { get; set; }
        public long TotalReceived { get; set; }
        public bool Success { get; set; }
    }
}
