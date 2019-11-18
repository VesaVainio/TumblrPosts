using System.Collections.Generic;
using Microsoft.Azure.CosmosDB.Table;
using Microsoft.Azure.Storage;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using TableInterface.Entities;

namespace TableInterface
{
    public class BlogInfoTableAdapter
    {
        private CloudTable blogsTable;
        private CloudTable monthIndexTable;

        public void Init()
        {
            string connectionString = ConfigurationManager.AppSettings["AzureWebJobsStorage"];
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

            blogsTable = tableClient.GetTableReference("Blogs");
            monthIndexTable = tableClient.GetTableReference("MonthIndex");
        }

        public void InsertBlog(BlogEntity blogEntity)
        {
            TableOperation insertOrMergeOperation = TableOperation.InsertOrMerge(blogEntity);
            blogsTable.Execute(insertOrMergeOperation);
        }

        public void InsertBlobStats(BlogStats blogStats)
        {
            TableOperation insertOrMergeOperation = TableOperation.InsertOrMerge(blogStats);
            blogsTable.Execute(insertOrMergeOperation);
        }

        public async Task<BlogEntity> GetBlog(string blogname)
        {
            TableOperation retrieveOperation = TableOperation.Retrieve<BlogEntity>(blogname, "info");
            TableResult result = await blogsTable.ExecuteAsync(retrieveOperation);
            if (result.HttpStatusCode == 200)
            {
                BlogEntity entity = (BlogEntity)result.Result;
                return entity;
            }

            return null;
        }

        public List<BlogStats> GetBlogStats()
        {
            string pkFilter = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, BlogStats.BlogStatsPartitionKey);
            TableQuery<BlogStats> query = new TableQuery<BlogStats>().Where(pkFilter);
            IEnumerable<BlogStats> result = blogsTable.ExecuteQuery(query);
            return result.ToList();
        }

        public void InsertMonthIndex(MonthIndex monthIndex)
        {
            TableOperation insertOrMergeOperation = TableOperation.InsertOrMerge(monthIndex);
            blogsTable.Execute(insertOrMergeOperation);
        }

        public void InsertMonthIndice(IEnumerable<MonthIndex> monthIndice)
        {
            TableBatchOperation batchOperation = new TableBatchOperation();

            int count = 0;
            foreach (MonthIndex item in monthIndice)
            {
                batchOperation.InsertOrMerge(item);
                count++;
                if (count == 100)
                {
                    monthIndexTable.ExecuteBatch(batchOperation);
                    batchOperation = new TableBatchOperation();
                    count = 0;
                }
            }

            if (count > 0)
            {
                monthIndexTable.ExecuteBatch(batchOperation);
            }
        }
    }
}
