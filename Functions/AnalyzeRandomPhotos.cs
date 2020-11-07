using System;
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
    public static class AnalyzeRandomPhotos
    {
        [FunctionName("AnalyzeRandomPhotos")]
        public static HttpResponseMessage Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "analyzerandomphotos")]
            HttpRequestMessage req, TraceWriter log)
        {
            Startup.Init();

            PostsTableAdapter postsTableAdapter = new PostsTableAdapter();
            postsTableAdapter.Init(log);

            ImageAnalysisTableAdapter imageAnalysisTableAdapter = new ImageAnalysisTableAdapter();
            imageAnalysisTableAdapter.Init();

            PhotoToAnalyzeQueueAdapter photoToAnalyzeQueueAdapter = new PhotoToAnalyzeQueueAdapter();
            photoToAnalyzeQueueAdapter.Init();

            string blobBaseUrl = ConfigurationManager.AppSettings["BlobBaseUrl"];

            int blogsLimit = 50;
            int photosInBlogLimit = 10;

            BlogInfoTableAdapter blogInfoTableAdapter = new BlogInfoTableAdapter();
            blogInfoTableAdapter.Init();

            List<BlogStats> blogStats = blogInfoTableAdapter.GetBlogStats();

            log.Info($"Got {blogStats.Count} blogs to index");

            Random random = new Random();
            blogStats.Shuffle(random);
            blogStats = blogStats.Take(blogsLimit).ToList();

            int totalCount = 0;

            foreach (string blogname in blogStats.Select(x => x.RowKey))
            {
                int analyzedInBlogCount = 0;
                List<PostEntity> noteCounts = postsTableAdapter.GetPostNoteCounts(blogname).OrderByDescending(x => x.NoteCount).ToList();
                log.Info($"Got note counts for {noteCounts.Count} posts in blog {blogname}");
                foreach (PostEntity noteCountPost in noteCounts)
                {
                    PostEntity postEntity = postsTableAdapter.GetPost(blogname, noteCountPost.RowKey);

                    if (postEntity == null)
                    {
                        log.Warning($"Post {blogname}/{noteCountPost.RowKey} not found, skipping");
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
                        analyzedInBlogCount++;
                        totalCount++;
                    }

                    if (analyzedInBlogCount >= photosInBlogLimit)
                    {
                        break;
                    }
                }
            }
                
            return req.CreateResponse(HttpStatusCode.OK, $"Will analyze {totalCount} new photos");
        }
    }
}