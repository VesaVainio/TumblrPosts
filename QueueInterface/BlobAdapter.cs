using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
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

            await blob.UploadFromByteArrayAsync(bytes, 0, bytes.Length);

            return blob.Uri;
        }

        public async Task<Uri> UploadVideoBlob(byte[] videoBytes, string blogName, string videoUrl)
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

            VideoUrlHelper videoUrlHelper = VideoUrlHelper.Parse(videoUrl);

            CloudBlockBlob blob;
            if (videoUrlHelper != null)
            {
                blob = serverContainer.GetBlockBlobReference(videoUrlHelper.FileName.ToLower());
            } else
            {
                blob = serverContainer.GetBlockBlobReference(Guid.NewGuid().ToString().ToLower());
            }

            await blob.UploadFromByteArrayAsync(videoBytes, 0, videoBytes.Length);

            return blob.Uri;
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
