using System.Configuration;
using Microsoft.Azure.CosmosDB.Table;
using Microsoft.Azure.Storage;
using TableInterface.Entities;

namespace TableInterface
{
    public class ImageAnalysisTableAdapter
    {
        private CloudTable imageAnalysisTable;

        public void Init()
        {
            string connectionString = ConfigurationManager.AppSettings["AzureWebJobsStorage"];
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

            imageAnalysisTable = tableClient.GetTableReference("ImageAnalysis");
        }

        public void InsertImageAnalysis(ImageAnalysisEntity imageAnalysisEntity)
        {
            TableOperation insertOrMergeOperation = TableOperation.InsertOrMerge(imageAnalysisEntity);
            imageAnalysisTable.Execute(insertOrMergeOperation);
        }
    }
}