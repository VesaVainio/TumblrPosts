using Microsoft.Azure.CosmosDB.Table;
using Microsoft.Azure.Storage;
using QueueInterface.Messages.Dto;
using System.Configuration;
using TableInterface.Entities;

namespace TableInterface
{
    public class LikeIndexTableAdapter
    {
        private CloudTable likeIndexTable;

        public void Init()
        {
            string connectionString = ConfigurationManager.AppSettings["AzureWebJobsStorage"];
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

            likeIndexTable = tableClient.GetTableReference("LikeIndex");
        }

        public void InsertLikeIndex(string blogName, string likedTimestamp, string likedBlogname, string likedPostId, string reblogKey)
        {
            LikeIndexEntity likeIndexEntity = new LikeIndexEntity(blogName, likedTimestamp, likedBlogname, likedPostId, reblogKey);

            TableOperation insertOrMergeOperation = TableOperation.InsertOrMerge(likeIndexEntity);
            likeIndexTable.Execute(insertOrMergeOperation);
        }
    }
}
