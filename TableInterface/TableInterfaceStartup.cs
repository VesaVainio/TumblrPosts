using Microsoft.Azure.CosmosDB.Table;
using Microsoft.Azure.Storage;
using System.Configuration;

namespace TableInterface
{
    public static class TableInterfaceStartup
    {
        public static void Init()
        {
            string connectionString = ConfigurationManager.AppSettings["AzureWebJobsStorage"];
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

            CloudTable photoIndexTable = tableClient.GetTableReference("PhotoIndex");
            photoIndexTable.CreateIfNotExists();

            CloudTable photoUrlIndexTable = tableClient.GetTableReference("PhotoUrlIndex");
            photoUrlIndexTable.CreateIfNotExists();

            CloudTable videoIndexTable = tableClient.GetTableReference("VideoIndex");
            videoIndexTable.CreateIfNotExists();

            CloudTable likeIndexTable = tableClient.GetTableReference("LikeIndex");
            likeIndexTable.CreateIfNotExists();

            CloudTable postsTable = tableClient.GetTableReference("Posts");
            postsTable.CreateIfNotExists();
        }
    }
}
