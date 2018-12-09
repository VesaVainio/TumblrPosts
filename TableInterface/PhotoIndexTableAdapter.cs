using Microsoft.Azure;
using Microsoft.Azure.Storage;
using Microsoft.Azure.CosmosDB.Table;
using TableInterface.Entities;
using QueueInterface.Messages.Dto;

namespace TableInterface
{
    public class PhotoIndexTableAdapter
    {
        private CloudTable photoIndexTable;

        public void Init()
        {
            string connectionString = CloudConfigurationManager.GetSetting("AzureWebJobsStorage");
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

            photoIndexTable = tableClient.GetTableReference("PhotoIndex");
            photoIndexTable.CreateIfNotExists();
        }

        public void InsertPhotoIndex(PostIndexInfo indexInfo, string uri, int width, int heigth)
        {
            PhotoIndexEntity photoIndexEntity = new PhotoIndexEntity(indexInfo.BlogName, indexInfo.PostId, indexInfo.PostDate)
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
