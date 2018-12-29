using Microsoft.Azure.CosmosDB.Table;
using Microsoft.Azure.Storage;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using TableInterface.Entities;

namespace TableInterface
{
    public class PhotoIndexTableAdapter
    {
        private CloudTable photoIndexTable;
        private CloudTable photoUrlIndexTable;

        public void Init()
        {
            string connectionString = ConfigurationManager.AppSettings["AzureWebJobsStorage"];
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

            photoIndexTable = tableClient.GetTableReference("PhotoIndex");
            photoUrlIndexTable = tableClient.GetTableReference("PhotoUrlIndex");
        }

        public void InsertPhotoIndex(string blogname, string id, DateTime date, string sourceBlog, string uri, string name, int size, int width, int heigth, string originalUrl)
        {
            PhotoIndexEntity photoIndexEntity = new PhotoIndexEntity(blogname, id, date, name, size)
            {
                Width = width,
                Height = heigth,
                Uri = uri
            };

            TableOperation insertOrMergeOperation = TableOperation.InsertOrMerge(photoIndexEntity);
            photoIndexTable.Execute(insertOrMergeOperation);

            PhotoUrlIndexEntity photoUrlIndexEntity = new PhotoUrlIndexEntity
            {
                PartitionKey = string.IsNullOrEmpty(sourceBlog) ? blogname : sourceBlog,
                RowKey = WebUtility.UrlEncode(originalUrl),
                BlobUrl = uri
            };

            insertOrMergeOperation = TableOperation.InsertOrMerge(photoUrlIndexEntity);
            photoUrlIndexTable.Execute(insertOrMergeOperation);
        }

        public PhotoUrlIndexEntity GetPhotoUrlndex(string sourceBlog, string photoUrl)
        {
            string encodedUrl = WebUtility.UrlEncode(photoUrl);
            TableOperation retrieveOperation = TableOperation.Retrieve<PhotoUrlIndexEntity>(sourceBlog, encodedUrl);
            TableResult result = photoUrlIndexTable.Execute(retrieveOperation);
            if (result.HttpStatusCode == 200)
            {
                PhotoUrlIndexEntity entity = (PhotoUrlIndexEntity)result.Result;
                return entity;
            }

            return null;
        }

        public List<PhotoIndexEntity> GetAll(string blogname)
        {
            string pkFilter = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, blogname);
            TableQuery<PhotoIndexEntity> query = new TableQuery<PhotoIndexEntity>().Where(pkFilter);
            IEnumerable<PhotoIndexEntity> result = photoIndexTable.ExecuteQuery(query);
            return result.ToList();
        }

        public List<PhotoIndexEntity> GetAll()
        {
            TableQuery<PhotoIndexEntity> query = new TableQuery<PhotoIndexEntity>();
            IEnumerable<PhotoIndexEntity> result = photoIndexTable.ExecuteQuery(query);
            return result.ToList();
        }
    }
}
