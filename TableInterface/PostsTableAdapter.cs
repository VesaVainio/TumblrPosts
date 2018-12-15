using Microsoft.Azure.CosmosDB.Table;
using Microsoft.Azure.Storage;
using Microsoft.Azure.WebJobs.Host;
using System.Configuration;
using TableInterface.Entities;
using TumblrPics.Model;

namespace TableInterface
{
    public class PostsTableAdapter
    {
        private CloudTable postsTable;
        private TraceWriter log;

        public void Init(TraceWriter log)
        {
            this.log = log;
            string connectionString = ConfigurationManager.AppSettings["AzureWebJobsStorage"];
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

            postsTable = tableClient.GetTableReference("Posts");
        }

        public bool InsertPost(PostEntity postEntity)
        {
            TableOperation insertOrMergeOperation = TableOperation.InsertOrMerge(postEntity);
            try
            {
                postsTable.Execute(insertOrMergeOperation);
            } catch (StorageException ex)
            {
                log.Warning("Saving PostEntity " + postEntity.PartitionKey + "/" + postEntity.RowKey + " failed: " + ex.Message);
                return false;
            }

            return true;
        }

        public PostEntity GetPost(string blogName, string postId)
        {
            TableOperation retrieveOperation = TableOperation.Retrieve<PostEntity>(blogName, postId);
            TableResult result = postsTable.Execute(retrieveOperation);
            if (result.HttpStatusCode == 200)
            {
                PostEntity entity = (PostEntity)result.Result;
                return entity;
            }

            return null;
        }

        public void MarkPhotosAsDownloaded(string blogName, string postId, string[] photoUrls)
        {
            PhotoDownloadCompleteEntity entity = new PhotoDownloadCompleteEntity
            {
                PartitionKey = blogName,
                RowKey = postId,
                PicsDownloadLevel = Constants.MaxPicsDownloadLevel,
                PhotoBlobUrls = string.Join(";", photoUrls)
            };

            TableOperation insertOrMergeOperation = TableOperation.InsertOrMerge(entity);
            postsTable.Execute(insertOrMergeOperation);
        }

        public void MarkVideosAsDownloaded(string blogName, string postId, string[] videoUrls)
        {
            VideoDownloadCompleteEntity entity = new VideoDownloadCompleteEntity
            {
                PartitionKey = blogName,
                RowKey = postId,
                VideosDownloadLevel = Constants.MaxVideosDownloadLevel,
                VideoBlobUrls = string.Join(";", videoUrls)
            };

            TableOperation insertOrMergeOperation = TableOperation.InsertOrMerge(entity);
            postsTable.Execute(insertOrMergeOperation);
        }
    }
}
