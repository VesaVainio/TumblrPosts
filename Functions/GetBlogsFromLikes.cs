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
                        BlogEntity blogEntity = new BlogEntity(blog);
                        blogInfoTableAdapter.InsertBlog(blogEntity);

                        blogStatsRow.HadPostCount = postsTableAdapter.GetPostCount(blogStatsRow.Blogname);
                        blogStatsRow.TotalPostCount = blog.Posts;
                        

                        long difference = blog.Posts - blogStatsRow.HadPostCount;
                        if (difference > 5)
                        {
                            log.Info("Blog " + blogStatsRow.Blogname + " to be downloaded, missing " + difference + " posts");
                            blogToFetchQueueAdapter.SendBlogToFetch(new BlogToFetch {
                                Blogname = blog.Name,
                                TotalPostCount = blog.Posts
                            });
                            toDownload.Add(blogStatsRow);
                        }
                        else
                        {
                            log.Info("Blog " + blogStatsRow.Blogname + " already downloaded (difference " + difference + ")");
                        }
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
