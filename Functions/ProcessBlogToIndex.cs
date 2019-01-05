using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Model.Site;
using Newtonsoft.Json;
using QueueInterface;
using QueueInterface.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using TableInterface;
using TableInterface.Entities;
using TumblrPics.Model;

namespace Functions
{
    public static class ProcessBlogToIndex
    {
        private static readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };

        [FunctionName("ProcessBlogToIndex")]
        public static void Run([QueueTrigger(Constants.BlogToIndexQueueName, Connection = "AzureWebJobsStorage")]string myQueueItem, TraceWriter log)
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

            List<PhotoIndexEntity> photoIndexEntities = photoIndexTableAdapter.GetAll(blogToIndex.Blogname);

            log.Info("Loaded " + photoIndexEntities.Count + " photo index entities");

            Dictionary<string, List<Photo>> photosByBlogById = CreatePhotosByBlogById(photoIndexEntities);
            PhotosByBlog photosByBlog = CreatePhotosByBlog(photoIndexEntities, blogToIndex.Blogname);

            List<PostEntity> postEntities = postsTableAdapter.GetAll(blogToIndex.Blogname);
            PostsByBlog postsByBlog = CreatePostsByBlog(postEntities, blogToIndex.Blogname);

            log.Info("Loaded " + postEntities.Count + " post entities");

            InsertReversePosts(blogToIndex.Blogname, photosByBlogById, postEntities, reversePostsTableAdapter, postToGetQueueAdapter, log);

            blogInfoTableAdapter.InsertPhotosByBlog(photosByBlog);
            blogInfoTableAdapter.InsertPostsByBlog(postsByBlog);
        }

        private static void InsertReversePosts(string blogname, Dictionary<string, List<Photo>> photosByBlogById, List<PostEntity> postEntities,
            ReversePostsTableAdapter reversePostsTableAdapter, PostToGetQueueAdapter postToGetQueueAdapter, TraceWriter log)
        {
            int index = 0;

            List<ReversePostEntity> reverseEntities = new List<ReversePostEntity>(100);

            foreach (PostEntity entity in postEntities)
            {
                if (entity.Type.Equals("Video", StringComparison.OrdinalIgnoreCase) && !entity.PostNotFound && 
                    (!entity.VideosDownloadLevel.HasValue || entity.VideosDownloadLevel.Value < Constants.MaxVideosDownloadLevel))
                {
                    postToGetQueueAdapter.Send(new PostToGet { Blogname = entity.PartitionKey, Id = entity.RowKey });
                }

                ReversePostEntity reversePost = new ReversePostEntity(entity.PartitionKey, entity.RowKey, entity.Type, entity.Date, entity.Body);
                List<Photo> photos = null;
                if (photosByBlogById.TryGetValue(entity.RowKey, out photos))
                {
                    reversePost.Photos = JsonConvert.SerializeObject(photos, JsonSerializerSettings);
                }

                reverseEntities.Add(reversePost);

                index++;
                if (index % 100 == 0)
                {
                    reversePostsTableAdapter.InsertBatch(reverseEntities);
                    reverseEntities.Clear();
                    log.Info("Inserted " + index + " reverse posts for " + entity.PartitionKey);
                }
            }

            reversePostsTableAdapter.InsertBatch(reverseEntities);
            log.Info("Inserted " + index + " reverse posts for " + blogname);
        }

        private static PostsByBlog CreatePostsByBlog(List<PostEntity> postEntities, string blogname)
        {
            PostsByBlog postsByBlog = new PostsByBlog(blogname);

            foreach (PostEntity post in postEntities)
            {
                switch (post.Type)
                {
                    case "Text":
                        {
                            postsByBlog.Text++;
                            break;
                        }
                    case "Quote":
                        {
                            postsByBlog.Quote++;
                            break;
                        }
                    case "Link":
                        {
                            postsByBlog.Link++;
                            break;
                        }
                    case "Answer":
                        {
                            postsByBlog.Answer++;
                            break;
                        }
                    case "Video":
                        {
                            postsByBlog.Video++;
                            break;
                        }
                    case "Audio":
                        {
                            postsByBlog.Audio++;
                            break;
                        }
                    case "Photo":
                        {
                            postsByBlog.Photo++;
                            break;
                        }
                    case "Chat":
                        {
                            postsByBlog.Chat++;
                            break;
                        }
                }

                postsByBlog.TotalPosts++;
            }

            return postsByBlog;
        }

        private static Dictionary<string, List<Photo>> CreatePhotosByBlogById(List<PhotoIndexEntity> photoIndexEntities)
        {
            Dictionary<string, List<Photo>> currentDictionary = new Dictionary<string, List<Photo>>();

            foreach (PhotoIndexEntity entity in photoIndexEntities)
            {
                PhotoUrlHelper urlHelper = PhotoUrlHelper.ParsePicai(entity.Uri);
                if (urlHelper != null)
                {
                    List<Photo> photosForId = null;
                    if (currentDictionary.TryGetValue(entity.PostId, out photosForId))
                    {
                        Photo photo = photosForId.FirstOrDefault(x => x.Name.Equals(urlHelper.Name));
                        if (photo == null)
                        {
                            photo = createPhoto(urlHelper, entity);
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
                        Photo photo = createPhoto(urlHelper, entity);
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

        private static PhotosByBlog CreatePhotosByBlog(List<PhotoIndexEntity> photoIndexEntities, string blogname)
        {
            PhotosByBlog currentBlog = new PhotosByBlog(blogname);

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
                    currentBlog.AverageWidth = currentBlog.TotalWidth / currentBlog.PhotosWithWidthCount;
                }
            }

            return currentBlog;
        }

        private static Photo createPhoto(PhotoUrlHelper urlHelper, PhotoIndexEntity entity)
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