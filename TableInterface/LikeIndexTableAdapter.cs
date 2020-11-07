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

        public List<LikeIndexEntity> GetNewerThan(string blogName, long timestamp)
        {
            string pkFilter = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, blogName);
            string rkFilter = TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.GreaterThan, timestamp.ToString());
            string combinedFilter = TableQuery.CombineFilters(pkFilter, TableOperators.And, rkFilter);
            TableQuery<LikeIndexEntity> query = new TableQuery<LikeIndexEntity>().Where(combinedFilter);
            IEnumerable<LikeIndexEntity> result = likeIndexTable.ExecuteQuery(query);
            List<LikeIndexEntity> entities = result.ToList();
            return entities;
        }

        public long GetNewestLikedTimestamp(string blogName)
        {
            string pkFilter = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, blogName);
            TableQuery query = new TableQuery().Where(pkFilter).Select(new List<string> { "LikedTimestamp" });
            IEnumerable<DynamicTableEntity> result = likeIndexTable.ExecuteQuery(query);
            DynamicTableEntity lastEntity = result.LastOrDefault();
            if (lastEntity != null)
            {
                return Convert.ToInt64(lastEntity.Properties["LikedTimestamp"].StringValue);
            }

            return -1;
        }
    }
}
