using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Newtonsoft.Json;
using TableInterface;

namespace PublicAPI
{
    public static class MonthIndex
    {
        private static readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };

        [FunctionName("MonthIndex")]
        public static HttpResponseMessage Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "monthindex/{blogname}")]
            HttpRequestMessage req, string blogname)
        {
            BlogInfoTableAdapter blogInfoTableAdapter = new BlogInfoTableAdapter();
            blogInfoTableAdapter.Init();

            List<TableInterface.Entities.MonthIndex> monthIndexEntities = blogInfoTableAdapter.GetMonthIndex(blogname);

            IEnumerable<Model.Site.MonthIndex> siteEntities = monthIndexEntities.Select(x => x.GetSiteEntity());

            string postsJson = JsonConvert.SerializeObject(siteEntities, JsonSerializerSettings);

            HttpResponseMessage response = req.CreateResponse(HttpStatusCode.OK);
            response.Content = new StringContent(postsJson, Encoding.UTF8, "application/json");
            return response;
        }
    }
}