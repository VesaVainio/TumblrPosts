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
using Model;
using QueueInterface;
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

            string sourceBlog = string.IsNullOrEmpty(videosToDownload.SourceBlog)
                ? videosToDownload.IndexInfo.BlogName
                : videosToDownload.SourceBlog;
            sourceBlog = SanityHelper.SanitizeSourceBlog(sourceBlog);

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
                        Video blobVideo = await blobAdapter.HandleVideo(videoUrls, videosToDownload.IndexInfo.BlogName, log);
                        videos.Add(blobVideo);

                        videoIndexTableAdapter.InsertVideoIndex(blogname, id, date, blobVideo, videosToDownload.VideoType, blobVideo.Bytes, videosToDownload.Duration);

                        log.Info("Video successfully downloaded: " + videoUrls.VideoUrl);
                    }
                    catch (HttpRequestException ex)
                    {
                        log.Warning("HTTP Error while downloading video " + videoUrls.VideoUrl + " - " + ex.Message);
                        postsTableAdapter.MarkWithVideoDownloadError(blogname, id, ex.Message);
                    }
                    catch (Exception ex)
                    {
                        log.Error("Error while downloading video ", ex);
                        throw;
                    }
                }

                if (videos.Count > 0)
                {
                    postsTableAdapter.MarkVideosAsDownloaded(videosToDownload.IndexInfo.BlogName, videosToDownload.IndexInfo.PostId, videos.ToArray());

                    ReversePostEntity reversePost = new ReversePostEntity(blogname, id, videosToDownload.PostType, date, videosToDownload.Body, videosToDownload.Title)
                    {
                        Videos = JsonConvert.SerializeObject(videos)
                    };
                    reversePostsTableAdapter.InsertPost(reversePost);
                }
            }
        }
    }
}
