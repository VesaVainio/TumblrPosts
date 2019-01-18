using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using TableInterface;
using TableInterface.Entities;

namespace PublicAPI
{
    public static class Posts
    {
        private static readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };

        [FunctionName("Posts")]
        public static HttpResponseMessage Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "posts/{blogname}")]HttpRequestMessage req,
            string blogname, TraceWriter log)
        {
            ReversePostsTableAdapter reversePostsTableAdapter = new ReversePostsTableAdapter();
            reversePostsTableAdapter.Init(log);

            string afterParam = req.GetQueryNameValuePairs().FirstOrDefault(q => q.Key.Equals("after", StringComparison.OrdinalIgnoreCase)).Value;

            List<ReversePostEntity> entities;
            if (string.IsNullOrEmpty(afterParam))
            {
                entities = reversePostsTableAdapter.GetMostRecent(blogname);
            }
            else
            {
                entities = reversePostsTableAdapter.GetAfter(blogname, afterParam);
            }

            List<Model.Site.Post> posts = entities.Select(x => x.GetSitePost()).ToList();

            string postsJson = JsonConvert.SerializeObject(posts, JsonSerializerSettings);

            HttpResponseMessage response = req.CreateResponse(HttpStatusCode.OK);
            response.Content = new StringContent(postsJson, Encoding.UTF8, "application/json");
            return response;
        }
    }
}
