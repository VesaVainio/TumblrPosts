using Microsoft.Azure;
using Microsoft.Azure.Storage;
using Microsoft.Azure.CosmosDB.Table;
using TableInterface.Entities;

namespace TableInterface
{
    public class PostsTableAdapter
    {
        private CloudTable postsTable;

        public void Init()
        {
            string connectionString = CloudConfigurationManager.GetSetting("AzureWebJobsStorage");
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

            postsTable = tableClient.GetTableReference("Posts");
            postsTable.CreateIfNotExists();
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

        public void MarkAsDownloaded(string blogName, string postId)
        {
            DownloadCompleteEntity entity = new DownloadCompleteEntity
            {
                PartitionKey = blogName,
                RowKey = postId,
                PicsDownloadLevel = 2
            };

            TableOperation insertOrMergeOperation = TableOperation.InsertOrMerge(entity);
            postsTable.Execute(insertOrMergeOperation);
        }
    }
}
