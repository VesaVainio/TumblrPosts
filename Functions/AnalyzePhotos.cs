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
    public static class AnalyzePhotos
    {
        [FunctionName("AnalyzePhotos")]
        public static HttpResponseMessage Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "analyze/{blogname}")]
            HttpRequestMessage req, string blogname, TraceWriter log)
        {
            Startup.Init();

            PostsTableAdapter postsTableAdapter = new PostsTableAdapter();
            postsTableAdapter.Init(log);

            PhotoToAnalyzeQueueAdapter photoToAnalyzeQueueAdapter = new PhotoToAnalyzeQueueAdapter();
            photoToAnalyzeQueueAdapter.Init();

            string blobBaseUrl = ConfigurationManager.AppSettings["BlobBaseUrl"];

            List<PostEntity> posts = postsTableAdapter.GetAll(blogname);

            log.Info($"Loaded {posts.Count} posts");

            int messageCount = 0;

            foreach (PostEntity postEntity in posts)
            {
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

            return req.CreateResponse(HttpStatusCode.OK, $"Processed {posts.Count} posts, sent {messageCount} messages");
        }
    }
}