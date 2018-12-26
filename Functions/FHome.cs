using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Model.Site;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using TableInterface;
using TableInterface.Entities;

namespace Functions
{
    public static class FHome
    {
        private static readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };

        [FunctionName("FHome")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "home/{blogname}")]HttpRequestMessage req, 
            string blogname, TraceWriter log)
        {
            Startup.Init();

            PostsTableAdapter postsTableAdapter = new PostsTableAdapter();
            postsTableAdapter.Init(log);

            List<PostEntity> entities = postsTableAdapter.GetMostRecent(blogname);

            IEnumerable<Post> posts = entities.Select(x => new Post
            {
                Blogname = x.PartitionKey,
                Id = x.RowKey,
                Type = x.Type,
                Date = x.Date,
                ImageUrl = GetImageUrl(x.PhotoBlobUrls)
            });

            return req.CreateResponse(HttpStatusCode.OK, entities, "application/json");
        }

        private static string GetImageUrl(string photoBlobUrls)
        {
            if (string.IsNullOrEmpty(photoBlobUrls))
            {
                return null;
            }

            string[] urls = photoBlobUrls.Split(';');

            return urls.Last();
        }
    }
}
