using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Model.Site;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using TableInterface;
using TableInterface.Entities;

namespace PublicAPI
{
    public static class Posts
    {
        [FunctionName("Posts")]
        public static HttpResponseMessage Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "posts/{blogname}")]HttpRequestMessage req, 
            string blogname, TraceWriter log)
        {
            ReversePostsTableAdapter reversePostsTableAdapter = new ReversePostsTableAdapter();
            reversePostsTableAdapter.Init(log);

            List<ReversePostEntity> entities = reversePostsTableAdapter.GetMostRecent(blogname);

            List<Post> posts = entities.Select(x => x.GetSitePost()).ToList();

            // parse query parameter
            string name = req.GetQueryNameValuePairs()
                .FirstOrDefault(q => string.Compare(q.Key, "name", true) == 0)
                .Value;

            return req.CreateResponse(HttpStatusCode.OK, posts, "application/json");
        }
    }
}