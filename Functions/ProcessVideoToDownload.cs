using BlobInterface;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Model.Site;
using Newtonsoft.Json;
using QueueInterface.Messages;
using QueueInterface.Messages.Dto;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using TableInterface;
using TableInterface.Entities;
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

            ReversePostsTableAdapter reversePostsTableAdapter = new ReversePostsTableAdapter();
            reversePostsTableAdapter.Init(log);

            using (HttpClient httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("video/*"));
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("image/*"));

                List<Video> videos = new List<Video>();

                string blogname = videosToDownload.IndexInfo.BlogName;
                string id = videosToDownload.IndexInfo.PostId;
                DateTime date = videosToDownload.IndexInfo.PostDate;

                foreach (VideoUrls videoUrls in videosToDownload.VideoUrls)
                {
                    try
                    {
                        byte[] videoBytes = await httpClient.GetByteArrayAsync(videoUrls.VideoUrl);
                        byte[] thumbBytes = await httpClient.GetByteArrayAsync(videoUrls.VideoThumbUrl);
                        if (videoBytes.Length > 0 && thumbBytes.Length > 0)
                        {
                            Video blobVideo = await blobAdapter.UploadVideoBlob(videoBytes, videosToDownload.IndexInfo.BlogName, videoUrls.VideoUrl, thumbBytes, videoUrls.VideoThumbUrl);
                            videos.Add(blobVideo);

                            videoIndexTableAdapter.InsertVideoIndex(blogname, id, date, blobVideo, videosToDownload.VideoType, videoBytes.Length, videosToDownload.Duration);

                            log.Info("Video successfully downloaded: " + videoUrls);
                        }
                    }
                    catch (HttpRequestException ex)
                    {
                        log.Warning("Error while downloading video " + videoUrls.VideoUrl + " - " + ex.Message);
                    }
                }

                if (videos.Count > 0)
                {
                    postsTableAdapter.MarkVideosAsDownloaded(videosToDownload.IndexInfo.BlogName, videosToDownload.IndexInfo.PostId, videos.ToArray());

                    ReversePostEntity reversePost = new ReversePostEntity(blogname, id, videosToDownload.PostType, date)
                    {
                        Videos = JsonConvert.SerializeObject(videos)
                    };
                    reversePostsTableAdapter.InsertPost(reversePost);
                }
            }
        }
    }
}
