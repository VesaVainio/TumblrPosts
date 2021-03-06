using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Model;
using Model.Site;
using Newtonsoft.Json;
using QueueInterface;
using QueueInterface.Messages;
using QueueInterface.Messages.Dto;
using TableInterface;
using TableInterface.Entities;
using TumblrPics.Model;
using MonthIndex = TableInterface.Entities.MonthIndex;
using Photo = Model.Tumblr.Photo;

namespace Functions
{
    public static class ProcessBlogToIndex
    {
        [FunctionName("ProcessBlogToIndex")]
        public static async Task Run([QueueTrigger(Constants.BlogToIndexQueueName, Connection = "AzureWebJobsStorage")]
            string myQueueItem, TraceWriter log)
        {
            Startup.Init();

            BlogToIndex blogToIndex = JsonConvert.DeserializeObject<BlogToIndex>(myQueueItem);

            PhotoIndexTableAdapter photoIndexTableAdapter = new PhotoIndexTableAdapter();
            photoIndexTableAdapter.Init();

            PostsTableAdapter postsTableAdapter = new PostsTableAdapter();
            postsTableAdapter.Init(log);

            ReversePostsTableAdapter reversePostsTableAdapter = new ReversePostsTableAdapter();
            reversePostsTableAdapter.Init(log);

            PostToGetQueueAdapter postToGetQueueAdapter = new PostToGetQueueAdapter();
            postToGetQueueAdapter.Init();

            BlogInfoTableAdapter blogInfoTableAdapter = new BlogInfoTableAdapter();
            blogInfoTableAdapter.Init();

            MediaToDownloadQueueAdapter mediaToDownloadQueueAdapter = new MediaToDownloadQueueAdapter();
            mediaToDownloadQueueAdapter.Init(log);

            List<PhotoIndexEntity> photoIndexEntities = photoIndexTableAdapter.GetAll(blogToIndex.Blogname);

            log.Info("Loaded " + photoIndexEntities.Count + " photo index entities");

            BlogEntity blogEntity = await blogInfoTableAdapter.GetBlog(blogToIndex.Blogname);

            Dictionary<string, List<Model.Site.Photo>> photosByBlogById = CreatePhotosByBlogById(photoIndexEntities);
            BlogStats blogStats = CreateBlogStatsFromPhotos(photoIndexEntities, blogToIndex.Blogname);
            blogStats.UpdateFromBlogEntity(blogEntity);

            List<PostEntity> postEntities = postsTableAdapter.GetAll(blogToIndex.Blogname);
            UpdateBlogStatsFromPosts(blogStats, postEntities);
            UpdateMonthIndex(blogToIndex.Blogname, postEntities, blogInfoTableAdapter);

            log.Info("Loaded " + postEntities.Count + " post entities");

            foreach (PostEntity postEntity in postEntities)
            {
                if (!string.IsNullOrEmpty(postEntity.PhotoBlobUrls))
                {
                    try
                    {
                        Model.Site.Photo[] photos = JsonConvert.DeserializeObject<Model.Site.Photo[]>(postEntity.PhotoBlobUrls);

                        if (photos.Any(x => !x.Name.Contains("_")))
                        {
                            SendToReprocessing(postEntity.PartitionKey, mediaToDownloadQueueAdapter, log, postEntity);
                        }
                    }
                    catch (Exception e)
                    {
                        log.Error("Error: " + e.Message);
                        throw;
                    }
                }
            }

            blogStats.DisplayablePosts = InsertReversePosts(blogToIndex.Blogname, photosByBlogById, postEntities, reversePostsTableAdapter,
                postsTableAdapter, photoIndexTableAdapter, mediaToDownloadQueueAdapter, log);

            blogInfoTableAdapter.InsertBlobStats(blogStats);
        }

        private static void UpdateMonthIndex(string blogname, List<PostEntity> postEntities, BlogInfoTableAdapter blogInfoTableAdapter)
        {
            Dictionary<string, MonthIndex> indexEntriesByMonth = new Dictionary<string, MonthIndex>();

            foreach (PostEntity postEntity in postEntities)
            {
                string monthKey = postEntity.Date.ToString(MonthIndex.MonthKeyFormat);
                if (!indexEntriesByMonth.TryGetValue(monthKey, out MonthIndex indexEntry))
                {
                    indexEntry = new MonthIndex(blogname, monthKey);
                    indexEntriesByMonth.Add(monthKey, indexEntry);
                }

                indexEntry.MonthsPosts++;

                long postId = long.Parse(postEntity.RowKey);
                if (indexEntry.FirstPostId == 0 || postId < indexEntry.FirstPostId)
                {
                    indexEntry.FirstPostId = ReversePostEntity.GetRowKeyId(postId);
                }
            }

            blogInfoTableAdapter.InsertMonthIndice(indexEntriesByMonth.Values);
        }

        private static void SendToReprocessing(string blogname, MediaToDownloadQueueAdapter mediaToDownloadQueueAdapter, TraceWriter log, PostEntity postEntity)
        {
            if (!string.IsNullOrEmpty(postEntity.PhotosJson))
            {
                SendPhotosToDownload(mediaToDownloadQueueAdapter, postEntity, JsonConvert.DeserializeObject<Photo[]>(postEntity.PhotosJson));
            }
            else if (!string.IsNullOrEmpty(postEntity.Body))
            {
                HtmlDocument htmlDoc = new HtmlDocument();
                string unescapedBody = JsonConvert.DeserializeObject<string>(postEntity.Body);
                htmlDoc.LoadHtml(unescapedBody);
                List<Photo> photosFromHtml = PostProcessor.ExctractPhotosFromHtml(htmlDoc);
                if (photosFromHtml.Count > 0)
                {
                    SendPhotosToDownload(mediaToDownloadQueueAdapter, postEntity, photosFromHtml.ToArray());
                }
                else
                {
                    log.Warning($"Post {blogname}/{postEntity.RowKey} has obsolete data and is missing PhotosJson and Body with photos");
                }
            }
            else
            {
                log.Warning($"Post {blogname}/{postEntity.RowKey} has obsolete data and is missing PhotosJson");
            }
        }

        private static void SendPhotosToDownload(MediaToDownloadQueueAdapter mediaToDownloadQueueAdapter, PostEntity postEntity, Photo[] photos)
        {
            mediaToDownloadQueueAdapter.SendPhotosToDownload(new PhotosToDownload
            {
                IndexInfo = new PostIndexInfo
                {
                    BlogName = postEntity.PartitionKey, PostId = postEntity.RowKey,
                    PostDate = postEntity.Date
                },
                ReblogKey = string.IsNullOrEmpty(postEntity.ReblogKey) ? null : postEntity.ReblogKey,
                SourceBlog = string.IsNullOrEmpty(postEntity.SourceTitle) ? null : postEntity.SourceTitle,
                PostType = postEntity.Type,
                Body = postEntity.Body,
                Title = postEntity.Title,
                Photos = photos
            });
        }

        private static int InsertReversePosts(string blogname, Dictionary<string, List<Model.Site.Photo>> photosByBlogById, List<PostEntity> postEntities,
            ReversePostsTableAdapter reversePostsTableAdapter, PostsTableAdapter postsTableAdapter,
            PhotoIndexTableAdapter photoIndexTableAdapter, MediaToDownloadQueueAdapter mediaToDownloadQueueAdapter, TraceWriter log)
        {
            int index = 0;

            List<ReversePostEntity> reverseEntities = new List<ReversePostEntity>(100);

            foreach (PostEntity entity in postEntities)
            {
                ReversePostEntity reversePost =
                    new ReversePostEntity(entity.PartitionKey, entity.RowKey, entity.Type, entity.Date, entity.ModifiedBody, entity.Title);
                if (photosByBlogById.TryGetValue(entity.RowKey, out List<Model.Site.Photo> photos))
                {
                    reversePost.Photos = JsonConvert.SerializeObject(photos, JsonUtils.JsonSerializerSettings);
                }
                else if (!string.IsNullOrEmpty(entity.VideoBlobUrls) && entity.VideoBlobUrls.StartsWith("[{"))
                {
                    reversePost.Videos = entity.VideoBlobUrls;
                }

                if (string.IsNullOrEmpty(entity.ModifiedBody) && !string.IsNullOrEmpty(entity.Body))
                {
                    string sourceBlog = string.IsNullOrEmpty(entity.SourceTitle) ? blogname : SanityHelper.SanitizeSourceBlog(entity.SourceTitle);

                    string modifiedBody = BodyUrlModifier.ModifyUrls(sourceBlog, entity.Body, photoIndexTableAdapter, photos, out List<Photo> extractedPhotos);
                    if (extractedPhotos != null && extractedPhotos.Count > 0)
                    {
                        PhotosToDownload photosToDownload = new PhotosToDownload(entity)
                        {
                            Photos = extractedPhotos.ToArray()
                        };
                        mediaToDownloadQueueAdapter.SendPhotosToDownload(photosToDownload);
                        log.Warning("Could not modify body successfully, sending PhotosToDownload to get missing photos");
                    }
                    else
                    {
                        entity.ModifiedBody = modifiedBody;

                        postsTableAdapter.InsertPost(entity);
                        log.Info($"ModifiedBody updated on post {entity.PartitionKey}/{entity.RowKey}");
                    }
                }

                if (!string.IsNullOrEmpty(reversePost.Photos) || !string.IsNullOrEmpty(reversePost.Videos) || !string.IsNullOrEmpty(reversePost.Body))
                {
                    reverseEntities.Add(reversePost);

                    index++;
                    if (index % 100 == 0)
                    {
                        reversePostsTableAdapter.InsertBatch(reverseEntities);
                        reverseEntities.Clear();
                        log.Info("Inserted " + index + " reverse posts for " + entity.PartitionKey);
                    }
                }
            }

            reversePostsTableAdapter.InsertBatch(reverseEntities);
            log.Info("Inserted " + index + " reverse posts for " + blogname);

            return index;
        }

        private static void UpdateBlogStatsFromPosts(BlogStats blogStats, List<PostEntity> postEntities)
        {
            foreach (PostEntity post in postEntities)
            {
                switch (post.Type)
                {
                    case "Text":
                    {
                        if (!string.IsNullOrEmpty(post.VideoBlobUrls))
                        {
                            blogStats.Video++;
                        }
                        else if (!string.IsNullOrEmpty(post.PhotoBlobUrls))
                        {
                            blogStats.Photo++;
                        }
                        else
                        {
                            blogStats.Text++;
                        }

                        break;
                    }
                    case "Quote":
                    {
                        blogStats.Quote++;
                        break;
                    }
                    case "Link":
                    {
                        blogStats.Link++;
                        break;
                    }
                    case "Answer":
                    {
                        blogStats.Answer++;
                        break;
                    }
                    case "Video":
                    {
                        blogStats.Video++;
                        break;
                    }
                    case "Audio":
                    {
                        blogStats.Audio++;
                        break;
                    }
                    case "Photo":
                    {
                        blogStats.Photo++;
                        break;
                    }
                    case "Chat":
                    {
                        blogStats.Chat++;
                        break;
                    }
                }

                blogStats.TotalPosts++;
            }
        }

        private static Dictionary<string, List<Model.Site.Photo>> CreatePhotosByBlogById(List<PhotoIndexEntity> photoIndexEntities)
        {
            Dictionary<string, List<Model.Site.Photo>> currentDictionary = new Dictionary<string, List<Model.Site.Photo>>();

            foreach (PhotoIndexEntity entity in photoIndexEntities)
            {
                PhotoUrlHelper urlHelper = PhotoUrlHelper.ParsePicai(entity.Uri);
                if (urlHelper != null)
                {
                    if (currentDictionary.TryGetValue(entity.PostId, out List<Model.Site.Photo> photosForId))
                    {
                        Model.Site.Photo photo = photosForId.FirstOrDefault(x => x.Name.Equals(urlHelper.Name));
                        if (photo == null)
                        {
                            photo = CreatePhoto(urlHelper, entity);
                            photosForId.Add(photo);
                        }
                        else
                        {
                            PhotoSize photoSize = new PhotoSize
                                { Container = urlHelper.Container, Nominal = urlHelper.Size, Heigth = entity.Height, Width = entity.Width };
                            photo.Sizes = photo.Sizes.Concat(new[] { photoSize }).ToArray();
                        }
                    }
                    else
                    {
                        photosForId = new List<Model.Site.Photo>();
                        Model.Site.Photo photo = CreatePhoto(urlHelper, entity);
                        photosForId.Add(photo);
                        currentDictionary.Add(entity.PostId, photosForId);
                    }
                }
                else
                {
                    throw new Exception("Parsing failed: " + entity.Uri);
                }
            }

            return currentDictionary;
        }

        private static BlogStats CreateBlogStatsFromPhotos(List<PhotoIndexEntity> photoIndexEntities, string blogname)
        {
            BlogStats currentBlog = new BlogStats(blogname);

            foreach (PhotoIndexEntity entity in photoIndexEntities)
            {
                if (entity.Uri.Contains("/thumb-"))
                {
                    continue; // process only originals
                }

                if (entity.Uri.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) || entity.Uri.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
                {
                    currentBlog.Jpgs += 1;
                }
                else if (entity.Uri.EndsWith(".gif", StringComparison.OrdinalIgnoreCase))
                {
                    currentBlog.Gifs += 1;
                }
                else if (entity.Uri.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                {
                    currentBlog.Pngs += 1;
                }
                else
                {
                    throw new InvalidOperationException("Unexpected ending");
                }

                currentBlog.PhotosCount += 1;

                if (entity.Width > 0)
                {
                    currentBlog.PhotosWithWidthCount += 1;
                    currentBlog.TotalWidth += entity.Width;
                    // ReSharper disable once PossibleLossOfFraction - fraction not needed here
                    currentBlog.AverageWidth = currentBlog.TotalWidth / currentBlog.PhotosWithWidthCount;
                }
            }

            return currentBlog;
        }

        private static Model.Site.Photo CreatePhoto(PhotoUrlHelper urlHelper, PhotoIndexEntity entity)
        {
            return new Model.Site.Photo
            {
                Name = urlHelper.Name,
                Extension = urlHelper.Extension,
                Sizes = new[] { new PhotoSize { Container = urlHelper.Container, Nominal = urlHelper.Size, Heigth = entity.Height, Width = entity.Width } }
            };
        }
    }
}