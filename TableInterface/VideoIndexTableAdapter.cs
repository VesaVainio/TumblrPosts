using Microsoft.Azure.CosmosDB.Table;
using Microsoft.Azure.Storage;
using Model.Site;
using System;
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

        public void InsertVideoIndex(string blogname, string id, DateTime date, Video video, string videoType, int bytes, double duration)
        {
            VideoIndexEntity videoIndexEntity = new VideoIndexEntity(blogname, id, date, bytes)
            {
                Uri = video.Url,
                ThumbUri = video.ThumbUrl,
                VideoType = videoType,
                Duration = duration
            };

            TableOperation insertOrMergeOperation = TableOperation.InsertOrMerge(videoIndexEntity);
            videoIndexTable.Execute(insertOrMergeOperation);
        }
    }
}
