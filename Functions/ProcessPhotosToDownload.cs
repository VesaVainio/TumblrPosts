using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Model;
using Model.Site;
using Newtonsoft.Json;
using QueueInterface;
using QueueInterface.Messages;
using TableInterface;
using TableInterface.Entities;
using TumblrPics.Model;
using TumblrPics.Model.Tumblr;
using Photo = Model.Site.Photo;

namespace Functions
{
    public static class ProcessPhotosToDownload
    {
        private static readonly int[] DownloadSizes = {1280, 640, 250};

        [FunctionName("ProcessPhotosToDownload")]
        public static async Task Run([QueueTrigger(Constants.PhotosToDownloadQueueName, Connection = "AzureWebJobsStorage")]
            string myQueueItem, TraceWriter log)
        {
            Startup.Init();

            try
            {
                PhotosToDownload photosToDownload = JsonConvert.DeserializeObject<PhotosToDownload>(myQueueItem);

                BlobAdapter blobAdapter = new BlobAdapter();
                blobAdapter.Init();

                PhotoIndexTableAdapter photoIndexTableAdapter = new PhotoIndexTableAdapter();
                photoIndexTableAdapter.Init();

                PostsTableAdapter postsTableAdapter = new PostsTableAdapter();
                postsTableAdapter.Init(log);

                ReversePostsTableAdapter reversePostsTableAdapter = new ReversePostsTableAdapter();
                reversePostsTableAdapter.Init(log);

                List<Photo> sitePhotos = new List<Photo>();

                string blogname = photosToDownload.IndexInfo.BlogName;
                string id = photosToDownload.IndexInfo.PostId;
                DateTime date = photosToDownload.IndexInfo.PostDate;

                string sourceBlog = string.IsNullOrEmpty(photosToDownload.SourceBlog)
                    ? photosToDownload.IndexInfo.BlogName
                    : photosToDownload.SourceBlog;
                sourceBlog = SanityHelper.SanitizeSourceBlog(sourceBlog);

                using (HttpClient httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("image/*"));

                    foreach (TumblrPics.Model.Tumblr.Photo photo in photosToDownload.Photos)
                    {
                        bool isOriginal = true;
                        Photo sitePhoto = null;

                        foreach (AltSize altSize in photo.Alt_sizes)
                        {
                            PhotoUrlHelper urlHelper = PhotoUrlHelper.ParseTumblr(altSize.Url);

                            if (isOriginal || urlHelper != null && DownloadSizes.Contains(urlHelper.Size))
                            {
                                if (sitePhoto == null)
                                    sitePhoto = new Photo
                                    {
                                        Name = urlHelper.Container + "_" + urlHelper.Name,
                                        Extension = urlHelper.Extension,
                                        Sizes = new PhotoSize[0]
                                    };

                                PhotoUrlIndexEntity urlIndexEntity = photoIndexTableAdapter.GetPhotoUrlndex(sourceBlog, altSize.Url);
                                if (urlIndexEntity != null) // photo already downloaded
                                {
                                    AddSizeToSitePhoto(sitePhoto, urlIndexEntity.BlobUrl, altSize); // need this to produce correct sitePhotos
                                    isOriginal = false;
                                }
                                else // photo not downloaded
                                {
                                    byte[] photoBytes = await httpClient.GetByteArrayAsync(altSize.Url);
                                    if (photoBytes.Length > 0)
                                    {
                                        Uri blobUri = await blobAdapter.UploadPhotoBlob(urlHelper, photoBytes, isOriginal);

                                        AddSizeToSitePhoto(sitePhoto, blobUri.ToString(), altSize);

                                        photoIndexTableAdapter.InsertPhotoIndex(blogname, id, date, SanityHelper.SanitizeSourceBlog(photosToDownload.SourceBlog),
                                            blobUri.ToString(), urlHelper.Name, urlHelper.Size,
                                            altSize.Width, altSize.Height, altSize.Url);
                                        isOriginal = false;
                                        log.Info("Downloaded photo from: " + altSize.Url);
                                    }
                                }
                            }
                        }

                        if (sitePhoto?.Sizes.Length > 0) sitePhotos.Add(sitePhoto);
                    }
                }

                string modifiedBody = BodyUrlModifier.ModifyUrls(sourceBlog, photosToDownload.Body, photoIndexTableAdapter, sitePhotos, log);

                postsTableAdapter.MarkPhotosAsDownloaded(photosToDownload.IndexInfo.BlogName, photosToDownload.IndexInfo.PostId, sitePhotos, modifiedBody);

                ReversePostEntity reversePost = new ReversePostEntity(photosToDownload.IndexInfo.BlogName, photosToDownload.IndexInfo.PostId,
                    photosToDownload.PostType, photosToDownload.IndexInfo.PostDate, modifiedBody, photosToDownload.Title)
                {
                    Photos = JsonConvert.SerializeObject(sitePhotos)
                };
                reversePostsTableAdapter.InsertPost(reversePost);
            }
            catch (Exception ex)
            {
                log.Error("Error in ProcessPhotosToDownload", ex);
            }
        }

        private static void AddSizeToSitePhoto(Photo sitePhoto, string blobUrl, AltSize altSize)
        {
            PhotoUrlHelper urlHelper = PhotoUrlHelper.ParsePicai(blobUrl);

            if (urlHelper != null)
            {
                PhotoSize photoSize = new PhotoSize
                {
                    Container = urlHelper.Container,
                    Nominal = urlHelper.Size,
                    Heigth = altSize.Height,
                    Width = altSize.Width
                };

                sitePhoto.Sizes = sitePhoto.Sizes.Concat(new PhotoSize[] {photoSize}).ToArray();
            }
        }
    }
}