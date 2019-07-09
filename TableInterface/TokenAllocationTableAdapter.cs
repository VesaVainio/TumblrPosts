using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using Microsoft.Azure.CosmosDB.Table;
using Microsoft.Azure.Storage;
using Model;
using TableInterface.Entities;

namespace TableInterface
{
    public class TokenAllocationTableAdapter
    {
        public const string PartitionDigram = "digram";
        public const string PartitionLabel = "label";

        private CloudTable tokenAllocationTable;

        public void Init()
        {
            string connectionString = ConfigurationManager.AppSettings["AzureWebJobsStorage"];
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

            tokenAllocationTable = tableClient.GetTableReference("TokenAllocation");
        }

        public List<FrequencyEntity> GetAllCountGte(string partition, int countLimit)
        {
            string pkFilter = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partition);
            string countFilter = TableQuery.GenerateFilterConditionForInt(nameof(FrequencyEntity.Count), QueryComparisons.GreaterThanOrEqual, countLimit);
            string combinedFilter = TableQuery.CombineFilters(pkFilter, TableOperators.And, countFilter);
            TableQuery<FrequencyEntity> query = new TableQuery<FrequencyEntity>().Where(combinedFilter);
            IEnumerable<FrequencyEntity> result = tokenAllocationTable.ExecuteQuery(query);
            List<FrequencyEntity> entities = result.ToList();
            return entities;
        }

        public void Update(IEnumerable<FrequencyEntity> entities)
        {
            TableBatchOperation batchOperation = new TableBatchOperation();

            int count = 0;
            foreach (FrequencyEntity frequencyEntity in entities)
            {
                batchOperation.InsertOrMerge(frequencyEntity);
                count++;
                if (count == 100)
                {
                    tokenAllocationTable.ExecuteBatch(batchOperation);
                    batchOperation = new TableBatchOperation();
                    count = 0;
                }
            }

            if (count > 0)
            {
                tokenAllocationTable.ExecuteBatch(batchOperation);
            }
        }

        public void InsertFrequencies(string partition, Dictionary<string, int> countsByKey)
        {
            TableBatchOperation batchOperation = new TableBatchOperation();

            int count = 0;
            foreach (KeyValuePair<string, int> item in countsByKey)
            {
                FrequencyEntity frequencyEntity = new FrequencyEntity
                {
                    PartitionKey = partition,
                    RowKey = StringTokenizer.SanitizeTableKey(item.Key, ""),
                    Count = item.Value
                };

                batchOperation.InsertOrMerge(frequencyEntity);
                count++;
                if (count == 100)
                {
                    tokenAllocationTable.ExecuteBatch(batchOperation);
                    batchOperation = new TableBatchOperation();
                    count = 0;
                }
            }

            if (count > 0)
            {
                tokenAllocationTable.ExecuteBatch(batchOperation);
            }
        }
    }
}