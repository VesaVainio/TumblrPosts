using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using BlobInterface;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using QueueInterface.Messages;
using TableInterface;
using TumblrPics.Model;

namespace Functions
{
    public static class ProcessVideosToDownload
    {
        [FunctionName("ProcessVideosToDownload")]
        public static async Task Run([QueueTrigger(Constants.VideosToDownloadQueueName, Connection = "AzureWebJobsStorage")]string myQueueItem, TraceWriter log)
        {
            Startup.Init();

            log.Info($"C# Queue trigger function processed: {myQueueItem}");

            VideosToDownload videosToDownload = JsonConvert.DeserializeObject<VideosToDownload>(myQueueItem);

            BlobAdapter blobAdapter = new BlobAdapter();
            blobAdapter.Init();

            VideoIndexTableAdapter videoIndexTableAdapter = new VideoIndexTableAdapter();
            videoIndexTableAdapter.Init();

            PostsTableAdapter postsTableAdapter = new PostsTableAdapter();
            postsTableAdapter.Init(log);

            using (HttpClient httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("video/*"));

                List<string> videoUris = new List<string>();

                foreach (string videoUrl in videosToDownload.VideoUrls)
                {
                    try
                    {
                        byte[] videoBytes = await httpClient.GetByteArrayAsync(videoUrl);
                        if (videoBytes.Length > 0)
                        {
                            Uri blobUri = await blobAdapter.UploadVideoBlob(videoBytes, videosToDownload.IndexInfo.BlogName, videoUrl);
                            videoUris.Add(blobUri.ToString());

                            videoIndexTableAdapter.InsertVideoIndex(videosToDownload.IndexInfo, blobUri.ToString(), videoUrl, videosToDownload.VideoType, videoBytes.Length, videosToDownload.Duration);

                            postsTableAdapter.MarkVideosAsDownloaded(videosToDownload.IndexInfo.BlogName, videosToDownload.IndexInfo.PostId, videoUris.ToArray());

                            log.Info("Video successfully downloaded: " + videoUrl);
                        }
                    }
                    catch (HttpRequestException ex)
                    {
                        log.Warning("Error while downloading video " + videoUrl + " - " + ex.Message);
                    }
                }
            }
        }
    }
}
