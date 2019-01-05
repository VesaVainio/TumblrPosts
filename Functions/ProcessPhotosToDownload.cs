using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using BlobInterface;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Model.Site;
using Newtonsoft.Json;
using QueueInterface.Messages;
using TableInterface;
using TableInterface.Entities;
using TumblrPics.Model;
using TumblrPics.Model.Tumblr;

namespace Functions
{
    public static class ProcessPhotosToDownload
    {
        private static int[] downloadSizes = { 1280, 640, 250 };

        [FunctionName("ProcessPhotosToDownload")]
        public static async Task Run([QueueTrigger(Constants.PhotosToDownloadQueueName, Connection = "AzureWebJobsStorage")]string myQueueItem, TraceWriter log)
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

                List<Model.Site.Photo> sitePhotos = new List<Model.Site.Photo>();

                using (HttpClient httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("image/*"));

                    foreach (TumblrPics.Model.Tumblr.Photo photo in photosToDownload.Photos)
                    {
                        bool isOriginal = true;
                        Model.Site.Photo sitePhoto = null;

                        foreach (AltSize altSize in photo.Alt_sizes)
                        {
                            PhotoUrlHelper urlHelper = PhotoUrlHelper.ParseTumblr(altSize.Url);

                            if (isOriginal || (urlHelper != null && downloadSizes.Contains(urlHelper.Size)))
                            {
                                if (sitePhoto == null)
                                {
                                    sitePhoto = new Model.Site.Photo
                                    {
                                        Name = urlHelper.Name,
                                        Extension = urlHelper.Extension,
                                        Sizes = new Model.Site.PhotoSize[0]
                                    };
                                }

                                string sourceBlog = string.IsNullOrEmpty(photosToDownload.SourceBlog) ? photosToDownload.IndexInfo.BlogName : photosToDownload.SourceBlog;
                                if (photoIndexTableAdapter.GetPhotoUrlndex(sourceBlog, altSize.Url) != null)
                                {
                                    log.Info($"Photo " + altSize.Url + " already downloaded");
                                    break;
                                }

                                byte[] photoBytes = await httpClient.GetByteArrayAsync(altSize.Url);
                                if (photoBytes.Length > 0)
                                {
                                    Uri blobUri = await blobAdapter.UploadPhotoBlob(urlHelper, photoBytes, isOriginal);

                                    AddSizeToSitePhoto(sitePhoto, blobUri, altSize);

                                    string blogname = photosToDownload.IndexInfo.BlogName;
                                    string id = photosToDownload.IndexInfo.PostId;
                                    DateTime date = photosToDownload.IndexInfo.PostDate;

                                    photoIndexTableAdapter.InsertPhotoIndex(blogname, id, date, photosToDownload.SourceBlog, blobUri.ToString(), urlHelper.Name, urlHelper.Size, 
                                        altSize.Width, altSize.Height, altSize.Url);
                                    isOriginal = false;
                                }
                            }
                            else
                            {
                                //log.Info($"Skipping AltSize with width {altSize.Width}");
                            }
                        }

                        if (sitePhoto.Sizes.Length > 0)
                        {
                            sitePhotos.Add(sitePhoto);
                        }
                    }
                }

                postsTableAdapter.MarkPhotosAsDownloaded(photosToDownload.IndexInfo.BlogName, photosToDownload.IndexInfo.PostId, sitePhotos);

                ReversePostEntity reversePost = new ReversePostEntity(photosToDownload.IndexInfo.BlogName, photosToDownload.IndexInfo.PostId, photosToDownload.PostType,
                    photosToDownload.IndexInfo.PostDate, photosToDownload.Body)
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

        private static void AddSizeToSitePhoto(Model.Site.Photo sitePhoto, Uri blobUri, AltSize altSize)
        {
            PhotoUrlHelper urlHelper = PhotoUrlHelper.ParsePicai(blobUri.ToString());

            if (urlHelper != null) {

                PhotoSize photoSize = new PhotoSize
                {
                    Container = urlHelper.Container,
                    Nominal = urlHelper.Size,
                    Heigth = altSize.Height,
                    Width = altSize.Width
                };

                sitePhoto.Sizes.Concat(new PhotoSize[] { photoSize });
            }
        }
    }
}
