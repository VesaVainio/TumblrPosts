using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Functions
{
    public static class GetPosts
    {
        [FunctionName("GetPosts")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "getposts/{blogname}")]HttpRequestMessage req, 
            string blogname, TraceWriter log)
        {
            Startup.Init();

            PostsGetter postsGetter = new PostsGetter();
            GetPostsResult result = await postsGetter.GetPosts(log, blogname);

            return req.CreateResponse(HttpStatusCode.OK, "Queued  " + result.TotalReceived + "/" + result.TotalInBlog + " posts for processing, success " + result.Success);
        }
    }
}
