using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Model.Site;
using Newtonsoft.Json;
using QueueInterface;
using QueueInterface.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using TableInterface;
using TableInterface.Entities;
using TumblrPics.Model;

namespace Functions
{
    public static class DoIndexing
    {
        private static readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };

        [FunctionName("DoIndexing")]
        public static HttpResponseMessage Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "index/{blogname}")]HttpRequestMessage req,
            string blogname, TraceWriter log)
        {
            Startup.Init();

            PhotoIndexTableAdapter photoIndexTableAdapter = new PhotoIndexTableAdapter();
            photoIndexTableAdapter.Init();

            PostsTableAdapter postsTableAdapter = new PostsTableAdapter();
            postsTableAdapter.Init(log);

            ReversePostsTableAdapter reversePostsTableAdapter = new ReversePostsTableAdapter();
            reversePostsTableAdapter.Init(log);

            PostToGetQueueAdapter postToGetQueueAdapter = new PostToGetQueueAdapter();
            postToGetQueueAdapter.Init();

            List<PhotoIndexEntity> photoIndexEntities = photoIndexTableAdapter.GetAll(blogname);

            log.Info("Loaded " + photoIndexEntities.Count + " photo index entities");

            Dictionary<string, Dictionary<string, List<Photo>>> photosByBlogById = CreatePhotosByBlogById(photoIndexEntities);
            Dictionary<string, PhotosByBlog> photosByBlog = CreatePhotosByBlog(photoIndexEntities);

            List<PostEntity> postEntities = postsTableAdapter.GetAll(blogname);

            log.Info("Loaded " + photoIndexEntities.Count + " photo index entities");

            InsertReversePosts(photosByBlogById, postEntities, reversePostsTableAdapter, postToGetQueueAdapter, log);


            return req.CreateResponse(HttpStatusCode.OK, "Indexed " + photoIndexEntities.Count + " photo entities");
        }

        private static void InsertReversePosts(Dictionary<string, Dictionary<string, List<Photo>>> photosByBlogById, List<PostEntity> postEntities,
            ReversePostsTableAdapter reversePostsTableAdapter, PostToGetQueueAdapter postToGetQueueAdapter, TraceWriter log)
        {
            foreach (PostEntity entity in postEntities)
            {
                if (entity.Type.Equals("Video", StringComparison.OrdinalIgnoreCase) && entity.VideosDownloadLevel < Constants.MaxVideosDownloadLevel)
                {
                    postToGetQueueAdapter.Send(new PostToGet { Blogname = entity.PartitionKey, Id = entity.RowKey });
                }

                ReversePostEntity reversePost = new ReversePostEntity(entity.PartitionKey, entity.RowKey, entity.Type, entity.Date, entity.Body);
                Dictionary<string, List<Photo>> dict = null;
                if (photosByBlogById.TryGetValue(entity.PartitionKey, out dict))
                {
                    List<Photo> photos = null;
                    if (dict.TryGetValue(entity.RowKey, out photos))
                    {
                        reversePost.Photos = JsonConvert.SerializeObject(photos, JsonSerializerSettings);
                    }
                }

                reversePostsTableAdapter.InsertPost(reversePost);
            }
        }

        private static Dictionary<string, Dictionary<string, List<Photo>>> CreatePhotosByBlogById(List<PhotoIndexEntity> photoIndexEntities)
        {
            Dictionary<string, Dictionary<string, List<Photo>>> photosByBlogById = new Dictionary<string, Dictionary<string, List<Photo>>>();
            Dictionary<string, List<Photo>> currentDictionary = null;

            foreach (PhotoIndexEntity entity in photoIndexEntities)
            {
                if (!photosByBlogById.TryGetValue(entity.PartitionKey, out currentDictionary))
                {
                    currentDictionary = new Dictionary<string, List<Photo>>();
                    photosByBlogById.Add(entity.PartitionKey, currentDictionary);
                }

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

            return photosByBlogById;
        }

        private static Dictionary<string, PhotosByBlog> CreatePhotosByBlog(List<PhotoIndexEntity> photoIndexEntities)
        {
            Dictionary<string, PhotosByBlog> photosByBlog = new Dictionary<string, PhotosByBlog>();
            PhotosByBlog currentBlog = null;

            foreach (PhotoIndexEntity entity in photoIndexEntities)
            {
                if (entity.Uri.Contains("/thumb-"))
                {
                    continue; // process only originals
                }

                if (!photosByBlog.TryGetValue(entity.PartitionKey, out currentBlog))
                {
                    currentBlog = new PhotosByBlog(entity.PartitionKey);
                    photosByBlog.Add(entity.PartitionKey, currentBlog);
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
                currentBlog.TotalWidth += entity.Width;
                currentBlog.AverageWidth = currentBlog.TotalWidth / currentBlog.PhotosCount;
            }

            return photosByBlog;
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
