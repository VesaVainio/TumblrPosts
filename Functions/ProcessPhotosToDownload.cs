using System;
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
            log.Info($"C# Queue trigger function ProcessPhotosToDownload processed: {myQueueItem}");

            PhotosToDownload photosToDownload = JsonConvert.DeserializeObject<PhotosToDownload>(myQueueItem);

            BlobAdapter blobAdapter = new BlobAdapter();
            blobAdapter.Init();

            PhotoIndexTableAdapter photoIndexTableAdapter = new PhotoIndexTableAdapter();
            photoIndexTableAdapter.Init();

            using (HttpClient httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("image/*"));

                foreach (Photo photo in photosToDownload.Photos)
                {
                    foreach (AltSize altSize in photo.Alt_sizes)
                    {
                        if (downloadSizes.Contains(altSize.Width))
                        {
                            byte[] photoBytes = await httpClient.GetByteArrayAsync(altSize.Url);
                            PhotoUrlHelper urlHelper = PhotoUrlHelper.Parse(altSize.Url);
                            if (urlHelper != null)
                            {
                                Uri blobUri = await blobAdapter.UploadBlob(urlHelper, photoBytes);
                                photoIndexTableAdapter.InsertPhotoIndex(photosToDownload.IndexInfo, blobUri.ToString(), altSize.Width, altSize.Height);
                            }
                            else
                            {
                                log.Info($"Unable to parse photo URL {altSize.Url}");
                            }
                        }
                        else
                        {
                            log.Info($"Skipping AltSize with width {altSize.Width}");
                        }
                    }
                }
            }
        }
    }
}
