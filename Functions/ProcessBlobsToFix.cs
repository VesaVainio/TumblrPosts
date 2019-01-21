using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using QueueInterface;
using QueueInterface.Messages;

namespace Functions
{
    //public static class ProcessBlobsToFix
    //{
    //    [FunctionName("ProcessBlobsToFix")]
    //    public static void Run([QueueTrigger("blobs-to-fix", Connection = "AzureWebJobsStorage")]string myQueueItem, TraceWriter log)
    //    {
    //        Startup.Init();

    //        BlobsToFix blobsToFix = JsonConvert.DeserializeObject<BlobsToFix>(myQueueItem);

    //        BlobAdapter blobAdapter = new BlobAdapter();
    //        blobAdapter.Init();

    //        blobAdapter.FixBlobs(blobsToFix, log);

    //        log.Info($"Fixed {blobsToFix.BlobNames.Count} blobs in {blobsToFix.Container}");
    //    }
    //}
}
