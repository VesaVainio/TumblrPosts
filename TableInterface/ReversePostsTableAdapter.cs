using Microsoft.Azure.CosmosDB.Table;
using Microsoft.Azure.Storage;
using Microsoft.Azure.WebJobs.Host;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using TableInterface.Entities;

namespace TableInterface
{
    public class ReversePostsTableAdapter
    {
        private CloudTable reversePostsTable;
        private TraceWriter log;

        public void Init(TraceWriter logWriter)
        {
            log = logWriter;
            string connectionString = ConfigurationManager.AppSettings["AzureWebJobsStorage"];
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

            reversePostsTable = tableClient.GetTableReference("ReversePosts");
        }

        public bool InsertPost(ReversePostEntity reversePostEntity)
        {
            TableOperation insertOrMergeOperation = TableOperation.InsertOrMerge(reversePostEntity);
            try
            {
                reversePostsTable.Execute(insertOrMergeOperation);
            } catch (StorageException ex)
            {
                log.Warning("Saving ReversePostEntity " + reversePostEntity.PartitionKey + "/" + reversePostEntity.RowKey + " failed: " + ex.Message);
                return false;
            }

            return true;
        }

        public void InsertBatch(IEnumerable<ReversePostEntity> reversePostEntities)
        {
            TableBatchOperation batchOperation = new TableBatchOperation();

            int count = 0;
            foreach (ReversePostEntity item in reversePostEntities)
            {
                batchOperation.InsertOrMerge(item);
                count++;
                if (count == 100)
                {
                    reversePostsTable.ExecuteBatch(batchOperation);
                    batchOperation = new TableBatchOperation();
                    count = 0;
                }
            }

            if (count > 0)
            {
                reversePostsTable.ExecuteBatch(batchOperation);
            }
        }

        public List<ReversePostEntity> GetMostRecent(string blogName, int maxCount = 20, int offset = 0)
        {
            string pkFilter = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, blogName);
            TableQuery<ReversePostEntity> query = new TableQuery<ReversePostEntity>().Where(pkFilter);
            IEnumerable<ReversePostEntity> result = reversePostsTable.ExecuteQuery(query);
            return result.Skip(offset).Take(maxCount).ToList();
        }

        public List<ReversePostEntity> GetAfter(string blogName, string afterId, int maxCount = 20)
        {
            string pkFilter = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, blogName);
            string rkFilter = TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.GreaterThan, afterId);
            string combinedFilter = TableQuery.CombineFilters(pkFilter, TableOperators.And, rkFilter);

            TableQuery<ReversePostEntity> query = new TableQuery<ReversePostEntity>().Where(combinedFilter);
            IEnumerable<ReversePostEntity> result = reversePostsTable.ExecuteQuery(query);
            return result.Take(maxCount).ToList();
        }

        public ReversePostEntity GetPost(string blogName, string id)
        {
            TableOperation retrieveOperation = TableOperation.Retrieve<ReversePostEntity>(blogName, id);
            TableResult result = reversePostsTable.Execute(retrieveOperation);
            if (result.HttpStatusCode == 200)
            {
                ReversePostEntity entity = (ReversePostEntity)result.Result;
                return entity;
            }

            return null;
        }
    }
}
