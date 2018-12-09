using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using QueueInterface;
using QueueInterface.Messages;
using TableInterface;
using TableInterface.Entities;
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

            BlogPosts blogPosts = null;
            using (HttpClient httpClient = new HttpClient())
            {
                string apiKey = ConfigurationManager.AppSettings["TumblrApiKey"];
                string url = "https://api.tumblr.com/v2/blog/" + blogname + "/posts?api_key=" + apiKey;
                HttpResponseMessage response = await httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    TumblrResponse<BlogPosts> tumblrResponse = await response.Content.ReadAsAsync<TumblrResponse<BlogPosts>>();
                    blogPosts = tumblrResponse.Response;
                }
            }

            Post testPost = blogPosts.Posts.First(x => x.Type == PostType.Photo);
            PostEntity postEntity = new PostEntity(testPost);

            PostsTableAdapter tableAdapter = new PostsTableAdapter();
            tableAdapter.Init();
            tableAdapter.InsertPost(postEntity);

            PhotosToDownload photosToDownloadMessage = new PhotosToDownload(testPost);

            QueueAdapter queueAdapter = new QueueAdapter();
            queueAdapter.Init();
            queueAdapter.SendPhotosToDownload(photosToDownloadMessage);

            log.Info("C# HTTP trigger function processed a request.");

            // Fetching the name from the path parameter in the request URL
            return req.CreateResponse(HttpStatusCode.OK, "Got " + blogPosts.Posts.Count + " posts");
        }
    }
}
