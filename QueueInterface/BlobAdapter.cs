using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Model.Site;
using System;
using System.Configuration;
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

        public async Task<Video> UploadVideoBlob(byte[] videoBytes, string blogName, string videoUrl, byte[] thumbBytes, string thumbUrl)
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

            CloudBlockBlob videoBlob = await HandleVideoBlob(videoBytes, videoUrl, serverContainer);
            CloudBlockBlob thumbBlob = await HandleThumbBlob(thumbBytes, thumbUrl, serverContainer);

            return new Video { Url = videoBlob.Uri.ToString(), ThumbUrl = thumbBlob.Uri.ToString() };
        }

        private static async Task<CloudBlockBlob> HandleVideoBlob(byte[] videoBytes, string videoUrl, CloudBlobContainer serverContainer)
        {
            VideoUrlHelper videoUrlHelper = VideoUrlHelper.Parse(videoUrl);

            CloudBlockBlob videoBlob;
            if (videoUrlHelper != null)
            {
                videoBlob = serverContainer.GetBlockBlobReference(videoUrlHelper.FileName.ToLower());
            }
            else
            {
                videoBlob = serverContainer.GetBlockBlobReference(Guid.NewGuid().ToString().ToLower() + ".mp4");
            }

            videoBlob.Properties.CacheControl = "max-age=31536000";
            if (videoUrl.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase))
            {
                videoBlob.Properties.ContentType = "video/mp4";
            }
            else
            {
                throw new ArgumentException("Unexpected ending in: " + videoUrl);
            }

            await videoBlob.UploadFromByteArrayAsync(videoBytes, 0, videoBytes.Length);
            return videoBlob;
        }

        private static async Task<CloudBlockBlob> HandleThumbBlob(byte[] thumbBytes, string thumbUrl, CloudBlobContainer serverContainer)
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

            await thumbBlob.UploadFromByteArrayAsync(thumbBytes, 0, thumbBytes.Length);
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
