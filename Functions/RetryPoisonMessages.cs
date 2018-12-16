using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using QueueInterface;
using System.Net;
using System.Net.Http;

namespace Functions
{
    public static class RetryPoisonMessages
    {
        [FunctionName("RetryPoisonMessages")]
        public static HttpResponseMessage Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]HttpRequestMessage req, TraceWriter log)
        {
            Startup.Init();

            RetryAdapter.RetryPoisonMessages(log);

            return req.CreateResponse(HttpStatusCode.OK, "Retrying poison messages done");
        }
    }
}
