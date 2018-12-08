
using Microsoft.Azure; // Namespace for CloudConfigurationManager
using Microsoft.Azure.Storage; // Namespace for StorageAccounts
using Microsoft.Azure.CosmosDB.Table; // Namespace for Table storage types

namespace TableInterface
{
    public class TableAdapter
    {
        private CloudTable postsTable;

        public void Init()
        {
            // Retrieve the storage account from the connection string.
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                CloudConfigurationManager.GetSetting("AzureWebJobsStorage"));

            // Create the table client.
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

            // Retrieve a reference to the table.
            postsTable = tableClient.GetTableReference("Posts");

            // Create the table if it doesn't exist.
            postsTable.CreateIfNotExists();
        }
    }
}
