using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Functions
{
    public static class AnalyzePhotos
    {
        [FunctionName("AnalyzePhotos")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "analyze/{blogname}")]HttpRequestMessage req, 
            string blogname, TraceWriter log)
        {
            Startup.Init();



            return req.CreateResponse(HttpStatusCode.OK, "Foobar");
        }
    }
}
