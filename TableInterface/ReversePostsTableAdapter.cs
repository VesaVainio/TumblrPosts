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
        private static List<string> PartitionAndRowKey = new List<string> { "PartitionKey", "RowKey" };
        private static List<string> FrontendColumns = new List<string> { "PartitionKey", "RowKey", "Date", "PhotoBlobUrls", "Type" };

        private CloudTable reversePostsTable;
        private TraceWriter log;

        public void Init(TraceWriter log)
        {
            this.log = log;
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

        public int GetPostCount(string blogName)
        {
            string pkFilter = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, blogName);
            TableQuery query = new TableQuery().Where(pkFilter).Select(PartitionAndRowKey);
            IEnumerable<DynamicTableEntity> result = reversePostsTable.ExecuteQuery(query);
            return result.Count();
        }

        public List<ReversePostEntity> GetAll(string blogname)
        {
            string pkFilter = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, blogname);
            TableQuery<ReversePostEntity> query = new TableQuery<ReversePostEntity>().Where(pkFilter);
            IEnumerable<ReversePostEntity> result = reversePostsTable.ExecuteQuery(query);
            return result.ToList();
        }

        public List<ReversePostEntity> GetAll()
        {
            TableQuery<ReversePostEntity> query = new TableQuery<ReversePostEntity>();
            IEnumerable<ReversePostEntity> result = reversePostsTable.ExecuteQuery(query);
            return result.ToList();
        }

        public List<ReversePostEntity> GetMostRecent(string blogName, int maxCount = 50, int offset = 0)
        {
            string pkFilter = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, blogName);
            TableQuery<ReversePostEntity> query = new TableQuery<ReversePostEntity>().Where(pkFilter).Select(FrontendColumns);
            IEnumerable<ReversePostEntity> result = reversePostsTable.ExecuteQuery(query);
            return result.Skip(offset).Take(maxCount).ToList();
        }
    }
}
