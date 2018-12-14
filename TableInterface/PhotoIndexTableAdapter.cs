using Microsoft.Azure.CosmosDB.Table;
using Microsoft.Azure.Storage;
using QueueInterface.Messages.Dto;
using System.Configuration;
using TableInterface.Entities;

namespace TableInterface
{
    public class PhotoIndexTableAdapter
    {
        private CloudTable photoIndexTable;

        public void Init()
        {
            string connectionString = ConfigurationManager.AppSettings["AzureWebJobsStorage"];
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

            photoIndexTable = tableClient.GetTableReference("PhotoIndex");
            photoIndexTable.CreateIfNotExists();
        }

        public void InsertPhotoIndex(PostIndexInfo indexInfo, string uri, int nominalSize, int width, int heigth)
        {
            PhotoIndexEntity photoIndexEntity = new PhotoIndexEntity(indexInfo.BlogName, indexInfo.PostId, indexInfo.PostDate, nominalSize)
            {
                Width = width,
                Height = heigth,
                Uri = uri
            };

            TableOperation insertOrMergeOperation = TableOperation.InsertOrMerge(photoIndexEntity);
            photoIndexTable.Execute(insertOrMergeOperation);
        }
    }
}
