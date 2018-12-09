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
    }
}
