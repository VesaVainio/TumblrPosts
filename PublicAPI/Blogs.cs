using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Model.Site;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using TableInterface;
using TableInterface.Entities;

namespace PublicAPI
{
    public static class Blogs
    {
        private static readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };

        [FunctionName("Blogs")]
        public static HttpResponseMessage Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "blogs")]HttpRequestMessage req, TraceWriter log)
        {
            BlogInfoTableAdapter blogInfoTableAdapter = new BlogInfoTableAdapter();
            blogInfoTableAdapter.Init();

            List<BlogStats> blogStats = blogInfoTableAdapter.GetBlogStats();

            List<BlogInfo> infos = blogStats.Select(x => x.GetSiteBlog()).ToList();

            string infosJson = JsonConvert.SerializeObject(infos, JsonSerializerSettings);

            HttpResponseMessage response = req.CreateResponse(HttpStatusCode.OK);
            response.Content = new StringContent(infosJson, Encoding.UTF8, "application/json");
            return response;
        }
    }
}
