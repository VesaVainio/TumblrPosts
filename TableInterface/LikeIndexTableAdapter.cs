using Microsoft.Azure.CosmosDB.Table;
using Microsoft.Azure.Storage;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
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
            TableResult result = likeIndexTable.Execute(insertOrMergeOperation);
            if (result.HttpStatusCode != 204 || ((LikeIndexEntity)result.Result).LikedBlogName == null)
            {
                throw new Exception("Failed!");
            }
        }

        public List<LikeIndexEntity> GetAll(string blogName)
        {
            string pkFilter = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, blogName);
            TableQuery<LikeIndexEntity> query = new TableQuery<LikeIndexEntity>().Where(pkFilter);
            IEnumerable<LikeIndexEntity> result = likeIndexTable.ExecuteQuery(query);
            List<LikeIndexEntity> entities = result.ToList();
            return entities;
        }
    }
}
