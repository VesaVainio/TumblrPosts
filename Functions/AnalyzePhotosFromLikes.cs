using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Model.Site;
using Newtonsoft.Json;
using QueueInterface;
using QueueInterface.Messages;
using TableInterface;
using TableInterface.Entities;

namespace Functions
{
    public static class AnalyzePhotosFromLikes
    {
        [FunctionName("AnalyzePhotosFromLikes")]
        public static HttpResponseMessage Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "analyze-from-likes/{blogname}")]
            HttpRequestMessage req, string blogname, TraceWriter log)
        {
            Startup.Init();

            LikeIndexTableAdapter likeIndexTableAdapter = new LikeIndexTableAdapter();
            likeIndexTableAdapter.Init();

            PostsTableAdapter postsTableAdapter = new PostsTableAdapter();
            postsTableAdapter.Init(log);

            ImageAnalysisTableAdapter imageAnalysisTableAdapter = new ImageAnalysisTableAdapter();
            imageAnalysisTableAdapter.Init();

            PhotoToAnalyzeQueueAdapter photoToAnalyzeQueueAdapter = new PhotoToAnalyzeQueueAdapter();
            photoToAnalyzeQueueAdapter.Init();

            string blobBaseUrl = ConfigurationManager.AppSettings["BlobBaseUrl"];

            List<LikeIndexEntity> likes = likeIndexTableAdapter.GetAll(blogname);

            log.Info($"Loaded {likes.Count} posts");

            int messageCount = 0;

            foreach (LikeIndexEntity like in likes)
            {
                if (like.LikedBlogName == null || like.LikedPostId == null)
                {
                    continue;
                }

                PostEntity postEntity = postsTableAdapter.GetPost(like.LikedBlogName, like.LikedPostId);

                if (postEntity == null)
                {
                    log.Warning($"Post {like.LikedBlogName}/{like.LikedPostId} not found, skipping");
                    continue;
                }

                if (string.IsNullOrEmpty(postEntity.PhotoBlobUrls))
                {
                    continue;
                }

                List<Photo> sitePhotos = JsonConvert.DeserializeObject<List<Photo>>(postEntity.PhotoBlobUrls);

                foreach (Photo photo in sitePhotos)
                {
                    List<PhotoSize> sortedSizes = photo.Sizes.OrderByDescending(x => x.Nominal).ToList();

                    PhotoSize original = sortedSizes.FirstOrDefault();
                    if (original == null)
                    {
                        continue;
                    }

                    string url = blobBaseUrl + "/" + original.Container + "/" + photo.Name + "_" + original.Nominal + "." + photo.Extension;

                    if (imageAnalysisTableAdapter.GetImageAnalysis(url) != null)
                    {
                        log.Info($"Image {url} already analyzed");
                        continue;
                    }

                    PhotoToAnalyze message = new PhotoToAnalyze
                    {
                        Blog = blogname,
                        PostDate = postEntity.Date,
                        Url = url
                    };
                    photoToAnalyzeQueueAdapter.Send(message);
                    log.Info($"Published PhotoToAnalyze message with URL {url}");
                    messageCount++;
                }
            }

            return req.CreateResponse(HttpStatusCode.OK, $"Processed {likes.Count} posts, sent {messageCount} messages");
        }
    }
}