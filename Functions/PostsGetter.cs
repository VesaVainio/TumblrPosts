using System;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host;
using QueueInterface;
using TableInterface;
using TableInterface.Entities;
using TumblrPics.Model;
using TumblrPics.Model.Tumblr;

namespace Functions
{
    public class PostsGetter
    {
        public async Task<GetPostsResult> GetPosts(TraceWriter log, string blogname, int startingOffset = 0, int maxOffset = Constants.MaxPostsToFetch,
            long timeoutSeconds = 270, bool updateNpf = false)
        {
            PostsToProcessQueueAdapter postsToProcessQueueAdapter = new PostsToProcessQueueAdapter();
            postsToProcessQueueAdapter.Init(log);

            BlogInfoTableAdapter blogInfoTableAdapter = new BlogInfoTableAdapter();
            blogInfoTableAdapter.Init();

            long totalInBlog = 0;
            long totalReceived = 0;
            Blog blog = null;
            bool success = true;
            int offset = startingOffset;

            using (HttpClient httpClient = new HttpClient())
            {
                Stopwatch stopwatch = Stopwatch.StartNew();

                string apiKey = ConfigurationManager.AppSettings["TumblrApiKey"];

                do
                {
                    string url = "https://api.tumblr.com/v2/blog/" + blogname + "/posts?npf=true&offset=" + offset + "&api_key=" + apiKey;
                    log.Info("Making request to: " + url);
                    HttpResponseMessage response = await httpClient.GetAsync(url);
                    if (response.IsSuccessStatusCode)
                    {
                        TumblrResponse<BlogPosts> tumblrResponse = await response.Content.ReadAsAsync<TumblrResponse<BlogPosts>>();
                        BlogPosts blogPosts = tumblrResponse.Response;

                        totalInBlog = blogPosts.Blog.Posts;
                        blog = blogPosts.Blog;
                        totalReceived += blogPosts.Posts.Count;
                        offset += 20;

                        if (blogPosts.Posts != null && blogPosts.Posts.Count > 0)
                        {
                            postsToProcessQueueAdapter.SendPostsToProcess(blogPosts.Posts);
                        }

                        if (updateNpf && blogPosts.Posts.Any(x => x.ShouldOpenInLegacy))
                        {
                            success = false;
                            break;
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

            BlogEntity blogEntity = new BlogEntity(blog)
            {
                FetchedUntilOffset = updateNpf ? (int?)null : offset,
                LastFetched = FunctionUtilities.GetUnixTime(DateTime.UtcNow)
            };
            blogInfoTableAdapter.InsertBlog(blogEntity);

            return new GetPostsResult
            {
                TotalInBlog = totalInBlog,
                TotalReceived = totalReceived,
                Success = success
            };
        }

        public async Task<GetPostsResult> GetNewerPosts(TraceWriter log, string blogname, long newerThan, long timeoutSeconds = 270)
        {
            PostsToProcessQueueAdapter postsToProcessQueueAdapter = new PostsToProcessQueueAdapter();
            postsToProcessQueueAdapter.Init(log);

            BlogInfoTableAdapter blogInfoTableAdapter = new BlogInfoTableAdapter();
            blogInfoTableAdapter.Init();

            long totalInBlog = 0;
            long totalReceived = 0;
            Blog blog = null;
            bool success = true;

            using (HttpClient httpClient = new HttpClient())
            {
                Stopwatch stopwatch = Stopwatch.StartNew();

                string apiKey = ConfigurationManager.AppSettings["TumblrApiKey"];

                string linkUrl = null;

                do
                {
                    string url;
                    if (linkUrl == null)
                    {
                        // start from newest posts
                        url = "https://api.tumblr.com/v2/blog/" + blogname + "/posts?npf=true&before=" + FunctionUtilities.GetUnixTime(DateTime.UtcNow) + "&api_key=" +
                              apiKey;
                    }
                    else
                    {
                        url = "https://api.tumblr.com" + linkUrl + "&api_key=" + apiKey;
                    }

                    log.Info("Making request to: " + url);
                    HttpResponseMessage response = await httpClient.GetAsync(url);
                    if (response.IsSuccessStatusCode)
                    {
                        TumblrResponse<BlogPosts> tumblrResponse = await response.Content.ReadAsAsync<TumblrResponse<BlogPosts>>();
                        BlogPosts blogPosts = tumblrResponse.Response;

                        totalInBlog = blogPosts.Blog.Posts;
                        blog = blogPosts.Blog;
                        totalReceived += blogPosts.Posts.Count;
                        if (blogPosts._links?.Next != null)
                        {
                            linkUrl = blogPosts._links.Next.Href;
                        }
                        else
                        {
                            linkUrl = null;
                        }

                        if (blogPosts.Posts != null && blogPosts.Posts.Count > 0)
                        {
                            if (blogPosts.Posts.Any(x => x.Timestamp < newerThan))
                            {
                                // have reached the point that was gotten previously
                                postsToProcessQueueAdapter.SendPostsToProcess(blogPosts.Posts.Where(x => x.Timestamp >= newerThan));
                                break;
                            }

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
                } while (linkUrl != null);
            }

            if (blog != null)
            {
                BlogEntity blogEntity = new BlogEntity(blog)
                {
                    LastFetched = FunctionUtilities.GetUnixTime(DateTime.UtcNow)
                };
                blogInfoTableAdapter.InsertBlog(blogEntity);
            }

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