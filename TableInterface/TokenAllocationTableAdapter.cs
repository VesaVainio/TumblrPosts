using System.Collections.Generic;
using System.Configuration;
using Microsoft.Azure.CosmosDB.Table;
using Microsoft.Azure.Storage;
using Model;
using TableInterface.Entities;

namespace TableInterface
{
    public class TokenAllocationTableAdapter
    {
        private CloudTable tokenAllocationTable;

        public void Init()
        {
            string connectionString = ConfigurationManager.AppSettings["AzureWebJobsStorage"];
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

            tokenAllocationTable = tableClient.GetTableReference("TokenAllocation");
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