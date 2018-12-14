using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using BlobInterface;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using QueueInterface.Messages;
using TableInterface;

namespace Functions
{
    public static class ProcessVideoToDownload
    {
        [FunctionName("ProcessVideoToDownload")]
        public static async Task Run([QueueTrigger("video-to-download", Connection = "AzureWebJobsStorage")]string myQueueItem, TraceWriter log)
        {
            log.Info($"C# Queue trigger function processed: {myQueueItem}");

            VideoToDownload videoToDownload = JsonConvert.DeserializeObject<VideoToDownload>(myQueueItem);

            BlobAdapter blobAdapter = new BlobAdapter();
            blobAdapter.Init();

            VideoIndexTableAdapter videoIndexTableAdapter = new VideoIndexTableAdapter();
            videoIndexTableAdapter.Init();

            PostsTableAdapter postsTableAdapter = new PostsTableAdapter();
            postsTableAdapter.Init(log);

            using (HttpClient httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("video/*"));

                byte[] videoBytes = await httpClient.GetByteArrayAsync(videoToDownload.VideoUrl);
                if (videoBytes.Length > 0)
                {
                    Uri blobUri = await blobAdapter.UploadVideoBlob(videoBytes, videoToDownload.IndexInfo.BlogName, videoToDownload.VideoUrl);
                    videoIndexTableAdapter.InsertVideoIndex(videoToDownload.IndexInfo, blobUri.ToString(), videoToDownload.VideoUrl, videoToDownload.VideoType, videoBytes.Length, videoToDownload.Duration);
                    log.Info("Video successfully downloaded: " + videoToDownload.VideoUrl);
                }
            }
        }
    }
}
