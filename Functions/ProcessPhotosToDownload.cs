using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using BlobInterface;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using QueueInterface.Messages;
using TableInterface;
using TumblrPics.Model;
using TumblrPics.Model.Tumblr;

namespace Functions
{
    public static class ProcessPhotosToDownload
    {
        private static int[] downloadSizes = { 1280, 640, 250 };

        [FunctionName("ProcessPhotosToDownload")]
        public static async Task Run([QueueTrigger("photos-to-download", Connection = "AzureWebJobsStorage")]string myQueueItem, TraceWriter log)
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

                List<string> blobUris = new List<string>();

                using (HttpClient httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("image/*"));

                    foreach (Photo photo in photosToDownload.Photos)
                    {
                        bool isOriginal = true;
                        foreach (AltSize altSize in photo.Alt_sizes)
                        {
                            PhotoUrlHelper urlHelper = PhotoUrlHelper.Parse(altSize.Url);

                            if (isOriginal || (urlHelper != null && downloadSizes.Contains(urlHelper.Size)))
                            {
                                string sourceBlog = string.IsNullOrEmpty(photosToDownload.SourceBlog) ? photosToDownload.IndexInfo.BlogName : photosToDownload.SourceBlog;
                                if (photoIndexTableAdapter.GetPhotoUrlndex(sourceBlog, altSize.Url) != null)
                                {
                                    log.Info($"Photo " + altSize.Url + "already downloaded");
                                    break;
                                }

                                byte[] photoBytes = await httpClient.GetByteArrayAsync(altSize.Url);
                                if (photoBytes.Length > 0)
                                {
                                    Uri blobUri = await blobAdapter.UploadPhotoBlob(urlHelper, photoBytes, isOriginal);
                                    if (isOriginal)
                                    {
                                        blobUris.Add(blobUri.ToString());
                                    }

                                    photoIndexTableAdapter.InsertPhotoIndex(photosToDownload.IndexInfo, photosToDownload.SourceBlog, blobUri.ToString(), urlHelper.Name, urlHelper.Size, 
                                        altSize.Width, altSize.Height, altSize.Url);
                                    isOriginal = false;
                                }
                            }
                            else
                            {
                                //log.Info($"Skipping AltSize with width {altSize.Width}");
                            }
                        }
                    }
                }

                postsTableAdapter.MarkPhotosAsDownloaded(photosToDownload.IndexInfo.BlogName, photosToDownload.IndexInfo.PostId, blobUris.ToArray());
            }
            catch (Exception ex)
            {
                log.Error("Error in ProcessPhotosToDownload", ex);
            }
        }
    }
}
