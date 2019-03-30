using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Net;
using Microsoft.Azure.CosmosDB.Table;
using Microsoft.Azure.Storage;
using TableInterface.Entities;

namespace TableInterface
{
    public class ImageAnalysisTableAdapter
    {
        private static readonly List<string> CanonicalColumns = new List<string> { "PartitionKey", "RowKey", "CanonicalJson" };

        private CloudTable blogImageAnalysisTable;
        private CloudTable imageAnalysisTable;

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
            TableEntity entity = new TableEntity { PartitionKey = blogname, RowKey = encodedUrl };

            TableOperation insertOrMergeOperation = TableOperation.InsertOrMerge(entity);
            blogImageAnalysisTable.Execute(insertOrMergeOperation);
        }

        public void InsertImageAnalysis(ImageAnalysisEntity imageAnalysisEntity, string photoUrl)
        {
            imageAnalysisEntity.PartitionKey = WebUtility.UrlEncode(photoUrl);
            imageAnalysisEntity.RowKey = "analysis";
            TableOperation insertOrMergeOperation = TableOperation.InsertOrMerge(imageAnalysisEntity);
            try
            {
                imageAnalysisTable.Execute(insertOrMergeOperation);
            }
            catch (Exception ex)
            {
                throw new DataException($"InsertImageAnalysis failed with URL {photoUrl}", ex);
            }
        }

        public void UpdateImageAnalysis(ImageAnalysisEntity imageAnalysisEntity)
        {
            if (imageAnalysisEntity.PartitionKey == null || imageAnalysisEntity.RowKey == null)
            {
                throw new InvalidOperationException("Can only update entities with valid PartitionKey and RowKey");
            }

            TableOperation mergeOperation = TableOperation.Merge(imageAnalysisEntity);
            imageAnalysisTable.Execute(mergeOperation);
        }

        public ImageAnalysisEntity GetImageAnalysis(string photoUrl)
        {
            string encodedUrl = WebUtility.UrlEncode(photoUrl);
            TableOperation retrieveOperation = TableOperation.Retrieve<ImageAnalysisEntity>(encodedUrl, "analysis");
            TableResult result = imageAnalysisTable.Execute(retrieveOperation);
            if (result.HttpStatusCode == 200)
            {
                ImageAnalysisEntity entity = (ImageAnalysisEntity) result.Result;
                return entity;
            }

            return null;
        }

        public List<ImageAnalysisEntity> GetAll()
        {
            TableQuery<ImageAnalysisEntity> query = new TableQuery<ImageAnalysisEntity>();
            IEnumerable<ImageAnalysisEntity> result = imageAnalysisTable.ExecuteQuery(query);
            return result.ToList();
        }

        public List<ImageAnalysisEntity> GetAllCanonical()
        {
            TableQuery<ImageAnalysisEntity> query = new TableQuery<ImageAnalysisEntity>().Select(CanonicalColumns);
            IEnumerable<ImageAnalysisEntity> result = imageAnalysisTable.ExecuteQuery(query);
            return result.ToList();
        }
    }
}