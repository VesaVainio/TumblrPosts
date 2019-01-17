using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using System.Net;
using System.Net.Http;
using System.Text;
using TableInterface;
using TableInterface.Entities;

namespace PublicAPI
{
    public static class Post
    {
        private static readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };

        [FunctionName("Post")]
        public static HttpResponseMessage Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "posts/{blogname}/{id}")]HttpRequestMessage req,
            string blogname, string id, TraceWriter log)
        {
            ReversePostsTableAdapter reversePostsTableAdapter = new ReversePostsTableAdapter();
            reversePostsTableAdapter.Init(log);

            ReversePostEntity entity = reversePostsTableAdapter.GetPost(blogname, id);

            Model.Site.Post post = entity != null ? entity.GetSitePost() : null;

            string postJson = JsonConvert.SerializeObject(post, JsonSerializerSettings);

            HttpResponseMessage response = req.CreateResponse(HttpStatusCode.OK);
            response.Content = new StringContent(postJson, Encoding.UTF8, "application/json");
            return response;
        }
    }
}
