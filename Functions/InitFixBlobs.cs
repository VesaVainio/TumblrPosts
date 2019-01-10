using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using QueueInterface;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using QueueInterface.Messages;

namespace Functions
{
    public static class InitFixBlobs
    {
        [FunctionName("InitFixBlobs")]
        public static HttpResponseMessage Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]HttpRequestMessage req, TraceWriter log)
        {
            BlobAdapter blobAdapter = new BlobAdapter();
            blobAdapter.Init();

            BlobsToFixQueueAdapter blobsToFixQueueAdapter = new BlobsToFixQueueAdapter();
            blobsToFixQueueAdapter.Init();

            Dictionary<string, List<string>> blobsByContainer = blobAdapter.GetBlobsByContainerMissingContentType();

            const int chunkSize = 500;

            foreach (KeyValuePair<string, List<string>> keyValuePair in blobsByContainer)
            {
                for (int i = 0; i < keyValuePair.Value.Count; i += chunkSize)
                {
                    List<string> blobs = keyValuePair.Value.GetRange(i, Math.Min(chunkSize, keyValuePair.Value.Count - i));
                    blobsToFixQueueAdapter.SendBlobsToFix(new BlobsToFix
                    {
                        Container = keyValuePair.Key,
                        BlobNames = blobs
                    });
                    log.Info($"Queued blobs {i} to {i + blobs.Count} in container {keyValuePair.Key}");
                }
            }

            return req.CreateResponse(HttpStatusCode.OK, "Ok");
        }
    }
}
