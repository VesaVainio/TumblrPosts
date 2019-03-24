using System.Configuration;
using System.Net;
using Microsoft.Azure.CosmosDB.Table;
using Microsoft.Azure.Storage;
using TableInterface.Entities;

namespace TableInterface
{
    public class ImageAnalysisTableAdapter
    {
        private CloudTable imageAnalysisTable;
        private CloudTable blogImageAnalysisTable;

        public void Init()
        {
            string connectionString = ConfigurationManager.AppSettings["AzureWebJobsStorage"];
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

            imageAnalysisTable = tableClient.GetTableReference("ImageAnalysis");
            blogImageAnalysisTable = tableClient.GetTableReference("BlogImageAnalysis");
        }

        public void InsertBlogImageAnalysis(string blogname, string photoUrl)
        {
            string encodedUrl = WebUtility.UrlEncode(photoUrl);
            TableEntity entity = new TableEntity {PartitionKey = blogname, RowKey = encodedUrl};

            TableOperation insertOrMergeOperation = TableOperation.InsertOrMerge(entity);
            blogImageAnalysisTable.Execute(insertOrMergeOperation);
        }

        public void InsertImageAnalysis(ImageAnalysisEntity imageAnalysisEntity, string photoUrl)
        {
            imageAnalysisEntity.PartitionKey = WebUtility.UrlEncode(photoUrl);
            imageAnalysisEntity.RowKey = "analysis";
            TableOperation insertOrMergeOperation = TableOperation.InsertOrMerge(imageAnalysisEntity);
            imageAnalysisTable.Execute(insertOrMergeOperation);
        }

        public ImageAnalysisEntity GetImageAnalysis(string photoUrl)
        {
            string encodedUrl = WebUtility.UrlEncode(photoUrl);
            TableOperation retrieveOperation = TableOperation.Retrieve<ImageAnalysisEntity>(encodedUrl, "analysis");
            TableResult result = imageAnalysisTable.Execute(retrieveOperation);
            if (result.HttpStatusCode == 200)
            {
                ImageAnalysisEntity entity = (ImageAnalysisEntity)result.Result;
                return entity;
            }

            return null;
        }
    }
}