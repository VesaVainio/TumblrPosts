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
        }

        public void InsertPhotoIndex(PostIndexInfo indexInfo, string uri, string name, int size, int width, int heigth)
        {
            PhotoIndexEntity photoIndexEntity = new PhotoIndexEntity(indexInfo.BlogName, indexInfo.PostId, indexInfo.PostDate, name, size)
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
