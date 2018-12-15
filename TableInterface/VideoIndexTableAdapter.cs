using Microsoft.Azure.CosmosDB.Table;
using Microsoft.Azure.Storage;
using QueueInterface.Messages.Dto;
using System.Configuration;
using TableInterface.Entities;

namespace TableInterface
{
    public class VideoIndexTableAdapter
    {
        private CloudTable videoIndexTable;

        public void Init()
        {
            string connectionString = ConfigurationManager.AppSettings["AzureWebJobsStorage"];
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

            videoIndexTable = tableClient.GetTableReference("VideoIndex");
        }

        public void InsertVideoIndex(PostIndexInfo indexInfo, string uri, string originalUri, string videoType, int bytes, int duration)
        {
            VideoIndexEntity videoIndexEntity = new VideoIndexEntity(indexInfo.BlogName, indexInfo.PostId, indexInfo.PostDate, bytes)
            {
                Uri = uri,
                OriginalUri = originalUri,
                VideoType = videoType,
                Duration = duration
            };

            TableOperation insertOrMergeOperation = TableOperation.InsertOrMerge(videoIndexEntity);
            videoIndexTable.Execute(insertOrMergeOperation);
        }
    }
}
