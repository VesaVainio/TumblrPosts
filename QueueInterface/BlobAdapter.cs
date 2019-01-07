using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Model.Site;
using QueueInterface.Messages.Dto;
using System;
using System.Configuration;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using TumblrPics.Model;

namespace BlobInterface
{
    public class BlobAdapter
    {
        private CloudBlobClient cloudBlobClient;

        public void Init()
        {
            string connectionString = ConfigurationManager.AppSettings["AzureWebJobsStorage"];
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);
            cloudBlobClient = storageAccount.CreateCloudBlobClient();
        }

        public async Task<Uri> UploadPhotoBlob(PhotoUrlHelper urlHelper, byte[] bytes, bool isOriginal)
        {
            string prefix = isOriginal ? "orig-" : "thumb-";
            CloudBlobContainer serverContainer = cloudBlobClient.GetContainerReference(prefix + urlHelper.Server.ToString());
            if (!await serverContainer.ExistsAsync())
            {
                await serverContainer.CreateAsync();

                // Set the permissions so the blobs are public. 
                BlobContainerPermissions permissions = new BlobContainerPermissions
                {
                    PublicAccess = BlobContainerPublicAccessType.Blob
                };
                await serverContainer.SetPermissionsAsync(permissions);
            }

            string path = GetPhotoPath(urlHelper);

            CloudBlockBlob blob = serverContainer.GetBlockBlobReference(path);
            blob.Properties.CacheControl = "max-age=31536000";
            blob.Properties.ContentType = GetContentType(path);

            await blob.UploadFromByteArrayAsync(bytes, 0, bytes.Length);

            return blob.Uri;
        }

        private static string GetContentType(string path)
        {
            if (path.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase))
            {
                return "image/jpg";
            }

            if (path.EndsWith(".gif", StringComparison.OrdinalIgnoreCase))
            {
                return "image/gif";
            }

            if (path.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            {
                return "image/png";
            }

            throw new ArgumentException("Unexpected ending in: " + path);
        }

        public async Task<Video> HandleVideo(VideoUrls videoUrls, string blogName)
        {
            CloudBlobContainer serverContainer = cloudBlobClient.GetContainerReference(blogName.ToLower());
            if (!await serverContainer.ExistsAsync())
            {
                await serverContainer.CreateAsync();

                // Set the permissions so the blobs are public. 
                BlobContainerPermissions permissions = new BlobContainerPermissions
                {
                    PublicAccess = BlobContainerPublicAccessType.Blob
                };
                await serverContainer.SetPermissionsAsync(permissions);
            }

            using (HttpClient httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("video/*"));
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("image/*"));

                CloudBlockBlob videoBlob = await HandleVideoBlob(videoUrls.VideoUrl, serverContainer, httpClient);
                CloudBlockBlob thumbBlob = await HandleThumbBlob(videoUrls.VideoThumbUrl, serverContainer, httpClient);

                return new Video { Url = videoBlob.Uri.ToString(), ThumbUrl = thumbBlob.Uri.ToString(), Bytes = videoBlob.Properties.Length };
            }
        }

        private static async Task<CloudBlockBlob> HandleVideoBlob(string videoUrl, CloudBlobContainer serverContainer, HttpClient httpClient)
        {
            VideoUrlHelper videoUrlHelper = VideoUrlHelper.Parse(videoUrl);

            CloudBlockBlob videoBlob;
            if (videoUrlHelper != null)
            {
                videoBlob = serverContainer.GetBlockBlobReference(videoUrlHelper.FileName.ToLower());
            }
            else
            {
                throw new ArgumentException("Unable to parse: " + videoUrl);
            }

            videoBlob.Properties.CacheControl = "max-age=31536000";
            if (videoUrlHelper.FileName.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase))
            {
                videoBlob.Properties.ContentType = "video/mp4";
            }
            else if (videoUrlHelper.FileName.EndsWith(".mov", StringComparison.OrdinalIgnoreCase))
            {
                videoBlob.Properties.ContentType = "video/quicktime";
            }
            else
            {
                throw new ArgumentException("Unexpected ending in: " + videoUrl);
            }

            var response = await httpClient.GetAsync(videoUrl);
            response.EnsureSuccessStatusCode();

            Stream sourceStream = null;
            CloudBlobStream uploadStream = null;
            try
            {
                sourceStream = await httpClient.GetStreamAsync(videoUrl);
                uploadStream = await videoBlob.OpenWriteAsync();
                byte[] bytes = new byte[64 * 1024];
                int length;
                do
                {
                    length = await sourceStream.ReadAsync(bytes, 0, 64 * 1024);
                    await uploadStream.WriteAsync(bytes, 0, length);
                }
                while (length > 0);
                
            }
            finally
            {
                if (sourceStream != null)
                {
                    sourceStream.Dispose();
                }
                if (uploadStream != null)
                {
                    await uploadStream.FlushAsync();
                    uploadStream.Dispose();
                }
            }

            await videoBlob.FetchAttributesAsync();

            return videoBlob;
        }

        private static async Task<CloudBlockBlob> HandleThumbBlob(string thumbUrl, CloudBlobContainer serverContainer, HttpClient httpClient)
        {
            VideoUrlHelper videoUrlHelper = VideoUrlHelper.Parse(thumbUrl);

            CloudBlockBlob thumbBlob;
            if (videoUrlHelper != null)
            {
                thumbBlob = serverContainer.GetBlockBlobReference(videoUrlHelper.FileName.ToLower());
            }
            else
            {
                throw new InvalidOperationException("Unable to parse thumb url: " + thumbUrl);
            }

            thumbBlob.Properties.CacheControl = "max-age=31536000";
            thumbBlob.Properties.ContentType = GetContentType(videoUrlHelper.FileName);

            Stream sourceStream = await httpClient.GetStreamAsync(thumbUrl);
            await thumbBlob.UploadFromStreamAsync(sourceStream);

            return thumbBlob;
        }

        private string GetPhotoPath(PhotoUrlHelper urlHelper)
        {
            StringBuilder sb = new StringBuilder();

            if (urlHelper.Container != null)
            {
                sb.Append(urlHelper.Container);
                sb.Append("_");
            }

            sb.Append(urlHelper.Name);
            sb.Append("_");

            sb.Append(urlHelper.Size);
            sb.Append(".");
            sb.Append(urlHelper.Extension);

            return sb.ToString();
        }
    }
}
