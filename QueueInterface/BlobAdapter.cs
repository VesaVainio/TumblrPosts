using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
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
            string connectionString = CloudConfigurationManager.GetSetting("AzureWebJobsStorage");
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);
            cloudBlobClient = storageAccount.CreateCloudBlobClient();
        }

        public async Task<Uri> UploadBlob(PhotoUrlHelper urlHelper, byte[] bytes)
        {
            CloudBlobContainer serverContainer = cloudBlobClient.GetContainerReference("server-" + urlHelper.Server.ToString());
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

            string path = GetPath(urlHelper);

            CloudBlockBlob blob = serverContainer.GetBlockBlobReference(path);

            await blob.UploadFromByteArrayAsync(bytes, 0, bytes.Length);

            return blob.Uri;
        }

        private string GetPath(PhotoUrlHelper urlHelper)
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
