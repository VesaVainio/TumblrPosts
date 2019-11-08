using System.Net;
using System.Net.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using QueueInterface;

namespace Functions
{
    public static class FixServiceProperties
    {
        [FunctionName("FixServiceProperties")]
        public static HttpResponseMessage Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]
            HttpRequestMessage req, TraceWriter log)
        {
            BlobAdapter blobAdapter = new BlobAdapter();
            blobAdapter.Init();

            blobAdapter.FixServiceProperties();

            return req.CreateResponse(HttpStatusCode.OK, "Service properties still todo");
        }
    }
}