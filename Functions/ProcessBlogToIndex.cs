using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Model.Site;
using Newtonsoft.Json;
using QueueInterface;
using QueueInterface.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HtmlAgilityPack;
using QueueInterface.Messages.Dto;
using TableInterface;
using TableInterface.Entities;
using TumblrPics.Model;

namespace Functions
{
    public static class ProcessBlogToIndex
    {
        private static readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };

        [FunctionName("ProcessBlogToIndex")]
        public static async Task Run([QueueTrigger(Constants.BlogToIndexQueueName, Connection = "AzureWebJobsStorage")]string myQueueItem, TraceWriter log)
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

            Dictionary<string, List<Photo>> photosByBlogById = CreatePhotosByBlogById(photoIndexEntities);
            BlogStats blogStats = CreateBlogStatsFromPhotos(photoIndexEntities, blogToIndex.Blogname);
            blogStats.UpdateFromBlogEntity(blogEntity);

            List<PostEntity> postEntities = postsTableAdapter.GetAll(blogToIndex.Blogname);
            UpdateBlogStatsFromPosts(blogStats, postEntities);
            UpdatePostEntities(blogToIndex.Blogname, postEntities, photosByBlogById, postsTableAdapter, mediaToDownloadQueueAdapter, log);

            log.Info("Loaded " + postEntities.Count + " post entities");

            blogStats.DisplayablePosts = InsertReversePosts(blogToIndex.Blogname, photosByBlogById, postEntities, reversePostsTableAdapter, postToGetQueueAdapter, log);

            blogInfoTableAdapter.InsertBlobStats(blogStats);
        }

        private static void UpdatePostEntities(string blogname, List<PostEntity> postEntities,
            Dictionary<string, List<Photo>> photosByBlogById, PostsTableAdapter postsTableAdapter,
            MediaToDownloadQueueAdapter mediaToDownloadQueueAdapter, TraceWriter log)
        {
            int index = 0;
            List<PostEntity> toUpdate = new List<PostEntity>(100);

            foreach (PostEntity postEntity in postEntities)
            {
                if (postEntity.PicsDownloadLevel < 4)
                {
                    if (photosByBlogById.TryGetValue(postEntity.RowKey, out List<Photo> photos))
                    {
                        postEntity.PhotoBlobUrls = JsonConvert.SerializeObject(photos, JsonSerializerSettings);
                        postEntity.PicsDownloadLevel = Constants.MaxPicsDownloadLevel;
                        toUpdate.Add(postEntity);

                        index++;
                        if (index % 100 == 0)
                        {
                            postsTableAdapter.InsertBatch(toUpdate);
                            toUpdate.Clear();
                            log.Info($"Updated {index} posts for {blogname}");
                        }
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(postEntity.PhotosJson))
                        {
                            SendPhotosToDownload(mediaToDownloadQueueAdapter, postEntity, JsonConvert.DeserializeObject<TumblrPics.Model.Tumblr.Photo[]>(postEntity.PhotosJson));
                        }
                        else if (!string.IsNullOrEmpty(postEntity.Body))
                        {
                            HtmlDocument htmlDoc = new HtmlDocument();
                            string unescapedBody = JsonConvert.DeserializeObject<string>(postEntity.Body);
                            htmlDoc.LoadHtml(unescapedBody);
                            List<TumblrPics.Model.Tumblr.Photo> photosFromHtml = PostProcessor.ExctractPhotosFromHtml(htmlDoc);
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
                }                
            }

            if (toUpdate.Count > 0)
            {
                postsTableAdapter.InsertBatch(toUpdate);
                log.Info($"Updated {index} posts for {blogname}");
            }
        }

        private static void SendPhotosToDownload(MediaToDownloadQueueAdapter mediaToDownloadQueueAdapter, PostEntity postEntity, TumblrPics.Model.Tumblr.Photo[] photos)
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
                Photos = photos
            });
        }

        private static int InsertReversePosts(string blogname, Dictionary<string, List<Photo>> photosByBlogById, List<PostEntity> postEntities,
            ReversePostsTableAdapter reversePostsTableAdapter, PostToGetQueueAdapter postToGetQueueAdapter, TraceWriter log)
        {
            int index = 0;

            List<ReversePostEntity> reverseEntities = new List<ReversePostEntity>(100);

            foreach (PostEntity entity in postEntities)
            {
                if (entity.Type.Equals("Video", StringComparison.OrdinalIgnoreCase) && !entity.PostNotFound && 
                    (entity.VideoType.Equals("tumblr", StringComparison.OrdinalIgnoreCase) || entity.VideoType.Equals("instagram", StringComparison.OrdinalIgnoreCase)) &&
                    (!entity.VideosDownloadLevel.HasValue || entity.VideosDownloadLevel.Value < Constants.MaxVideosDownloadLevel))
                {
                    postToGetQueueAdapter.Send(new PostToGet { Blogname = entity.PartitionKey, Id = entity.RowKey });
                }

                ReversePostEntity reversePost = new ReversePostEntity(entity.PartitionKey, entity.RowKey, entity.Type, entity.Date, entity.Body);
                if (photosByBlogById.TryGetValue(entity.RowKey, out List<Photo> photos))
                {
                    reversePost.Photos = JsonConvert.SerializeObject(photos, JsonSerializerSettings);
                } else if (!string.IsNullOrEmpty(entity.VideoBlobUrls))
                {
                    // TODO check that VideoBlobUrls is valid JSON, not all of them are!
                    reversePost.Videos = entity.VideoBlobUrls;
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
                else
                {
                    log.Warning($"Post {entity.PartitionKey}/{entity.RowKey} skipped as it has no Photos, Videos or Body");
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
                            blogStats.Text++;
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

        private static Dictionary<string, List<Photo>> CreatePhotosByBlogById(List<PhotoIndexEntity> photoIndexEntities)
        {
            Dictionary<string, List<Photo>> currentDictionary = new Dictionary<string, List<Photo>>();

            foreach (PhotoIndexEntity entity in photoIndexEntities)
            {
                PhotoUrlHelper urlHelper = PhotoUrlHelper.ParsePicai(entity.Uri);
                if (urlHelper != null)
                {
                    if (currentDictionary.TryGetValue(entity.PostId, out List<Photo> photosForId))
                    {
                        Photo photo = photosForId.FirstOrDefault(x => x.Name.Equals(urlHelper.Name));
                        if (photo == null)
                        {
                            photo = CreatePhoto(urlHelper, entity);
                            photosForId.Add(photo);
                        }
                        else
                        {
                            PhotoSize photoSize = new PhotoSize { Container = urlHelper.Container, Nominal = urlHelper.Size, Heigth = entity.Height, Width = entity.Width };
                            photo.Sizes = photo.Sizes.Concat(new[] { photoSize }).ToArray();
                        }
                    }
                    else
                    {
                        photosForId = new List<Photo>();
                        Photo photo = CreatePhoto(urlHelper, entity);
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

        private static Photo CreatePhoto(PhotoUrlHelper urlHelper, PhotoIndexEntity entity)
        {
            return new Photo
            {
                Name = urlHelper.Name,
                Extension = urlHelper.Extension,
                Sizes = new[] { new PhotoSize { Container = urlHelper.Container, Nominal = urlHelper.Size, Heigth = entity.Height, Width = entity.Width } }
            };
        }
    }
}