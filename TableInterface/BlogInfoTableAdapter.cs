using Microsoft.Azure.CosmosDB.Table;
using Microsoft.Azure.Storage;
using System.Configuration;
using System.Threading.Tasks;
using TableInterface.Entities;

namespace TableInterface
{
    public class BlogInfoTableAdapter
    {
        private CloudTable blogsTable;

        public void Init()
        {
            string connectionString = ConfigurationManager.AppSettings["AzureWebJobsStorage"];
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

            blogsTable = tableClient.GetTableReference("Blogs");
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
    }
}
