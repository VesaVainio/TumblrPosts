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

        public void Init(TraceWriter log)
        {
            string connectionString = ConfigurationManager.AppSettings["AzureWebJobsStorage"];
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

            postsTable = tableClient.GetTableReference("Posts");
        }

        public void InsertPost(PostEntity postEntity)
        {
            TableOperation insertOrMergeOperation = TableOperation.InsertOrMerge(postEntity);
            postsTable.Execute(insertOrMergeOperation);
        }

        public PostEntity GetPost(string blogName, string postId)
        {
            TableOperation retrieveJeffSmith = TableOperation.Retrieve<PostEntity>(blogName, postId);
            TableResult result = postsTable.Execute(retrieveJeffSmith);
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
