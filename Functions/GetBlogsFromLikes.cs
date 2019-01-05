using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using QueueInterface;
using QueueInterface.Messages;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using TableInterface;
using TableInterface.Entities;
using TumblrPics.Model;
using TumblrPics.Model.Tumblr;

namespace Functions
{
    public static class GetBlogsFromLikes
    {
        [FunctionName("GetBlogsFromLikes")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "getblogsfromlikes/{blogname}")]HttpRequestMessage req,
            string blogname, TraceWriter log)
        {
            Startup.Init();

            LikeIndexTableAdapter likeIndexTableAdapter = new LikeIndexTableAdapter();
            likeIndexTableAdapter.Init();

            BlogInfoTableAdapter blogInfoTableAdapter = new BlogInfoTableAdapter();
            blogInfoTableAdapter.Init();

            PostsTableAdapter postsTableAdapter = new PostsTableAdapter();
            postsTableAdapter.Init(log);

            BlogToFetchQueueAdapter blogToFetchQueueAdapter = new BlogToFetchQueueAdapter();
            blogToFetchQueueAdapter.Init();

            List<LikeIndexEntity> entities = likeIndexTableAdapter.GetAll(blogname);
            ILookup<string, LikeIndexEntity> likesByBlog = entities.ToLookup(e => e.LikedBlogName);
            List<BlogStatsRow> stats = likesByBlog.Select(gr => new BlogStatsRow { Blogname = gr.Key, LikedPostCount = gr.Count() }).OrderByDescending(x => x.LikedPostCount).ToList();

            string apiKey = ConfigurationManager.AppSettings["TumblrApiKey"];

            List<BlogStatsRow> toDownload = new List<BlogStatsRow>();

            using (HttpClient httpClient = new HttpClient())
            {
                foreach (BlogStatsRow blogStatsRow in stats)
                {
                    if (blogStatsRow.LikedPostCount < 5)
                    {
                        break;
                    }

                    string url = "https://api.tumblr.com/v2/blog/" + blogStatsRow.Blogname + "/info?api_key=" + apiKey;
                    log.Info("Making request to: " + url);
                    HttpResponseMessage response = await httpClient.GetAsync(url);
                    if (response.IsSuccessStatusCode)
                    {
                        TumblrResponse<BlogInfo> tumblrResponse = await response.Content.ReadAsAsync<TumblrResponse<BlogInfo>>();
                        Blog blog = tumblrResponse.Response.Blog;
                        BlogEntity blogEntity = await blogInfoTableAdapter.GetBlog(blogStatsRow.Blogname);

                        blogStatsRow.HadPostCount = postsTableAdapter.GetPostCount(blogStatsRow.Blogname);
                        blogStatsRow.TotalPostCount = blog.Posts;
                        long difference = blog.Posts - blogStatsRow.HadPostCount;
                        bool fetch = false;
                        long? newerThan = null;

                        if (blogEntity != null && blogEntity.Updated < blog.Updated)
                        {
                            log.Info("Blog " + blogStatsRow.Blogname + " to be downloaded, has new posts");
                            fetch = true;
                            newerThan = blogEntity.Updated;
                        }
                        else if (blogStatsRow.HadPostCount > Constants.MaxPostsToFetch)
                        {
                            log.Info("Already fetched " + blogStatsRow.HadPostCount + " posts from blog");
                        }
                        else if (difference > 5)
                        {
                            log.Info("Blog " + blogStatsRow.Blogname + " to be downloaded, missing " + difference + " posts");
                            fetch = true;
                        }
                        else
                        {
                            log.Info("Blog " + blogStatsRow.Blogname + " already downloaded (difference " + difference + ")");
                        }

                        if (fetch)
                        {
                            blogToFetchQueueAdapter.SendBlogToFetch(new BlogToFetch
                            {
                                Blogname = blog.Name,
                                TotalPostCount = blog.Posts,
                                NewerThan = newerThan
                            });
                            toDownload.Add(blogStatsRow);
                        }

                        blogEntity = new BlogEntity(blog);
                        blogInfoTableAdapter.InsertBlog(blogEntity);
                    }
                }
            }

            return req.CreateResponse(HttpStatusCode.OK, "Got " + toDownload.Count + " blogs to fetch");
        }
    }

    class BlogStatsRow
    {
        public string Blogname { get; set; }
        public int LikedPostCount { get; set; }
        public int HadPostCount { get; set; }
        public long TotalPostCount { get; set; }
    }
}
