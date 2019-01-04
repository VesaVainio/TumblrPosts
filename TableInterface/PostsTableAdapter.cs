using Microsoft.Azure.CosmosDB.Table;
using Microsoft.Azure.Storage;
using Microsoft.Azure.WebJobs.Host;
using Model.Site;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using TableInterface.Entities;
using TableInterface.Entities.Partial;
using TumblrPics.Model;

namespace TableInterface
{
    public class PostsTableAdapter
    {
        private static readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };

        private static List<string> PartitionAndRowKey = new List<string> { "PartitionKey", "RowKey" };
        private static List<string> FrontendColumns = new List<string> { "PartitionKey", "RowKey", "Date", "PhotoBlobUrls", "Type" };

        private CloudTable postsTable;
        private TraceWriter log;

        public void Init(TraceWriter log)
        {
            this.log = log;
            string connectionString = ConfigurationManager.AppSettings["AzureWebJobsStorage"];
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

            postsTable = tableClient.GetTableReference("Posts");
        }

        public bool InsertPost(PostEntity postEntity)
        {
            TableOperation insertOrMergeOperation = TableOperation.InsertOrMerge(postEntity);
            try
            {
                postsTable.Execute(insertOrMergeOperation);
            } catch (StorageException ex)
            {
                log.Warning("Saving PostEntity " + postEntity.PartitionKey + "/" + postEntity.RowKey + " failed: " + ex.Message);
                return false;
            }

            return true;
        }

        public PostEntity GetPost(string blogName, string postId)
        {
            TableOperation retrieveOperation = TableOperation.Retrieve<PostEntity>(blogName, postId);
            TableResult result = postsTable.Execute(retrieveOperation);
            if (result.HttpStatusCode == 200)
            {
                PostEntity entity = (PostEntity)result.Result;
                return entity;
            }

            return null;
        }

        public int GetPostCount(string blogName)
        {
            string pkFilter = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, blogName);
            TableQuery query = new TableQuery().Where(pkFilter).Select(PartitionAndRowKey);
            IEnumerable<DynamicTableEntity> result = postsTable.ExecuteQuery(query);
            return result.Count();
        }

        public List<PostEntity> GetAll(string blogname)
        {
            string pkFilter = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, blogname);
            TableQuery<PostEntity> query = new TableQuery<PostEntity>().Where(pkFilter);
            IEnumerable<PostEntity> result = postsTable.ExecuteQuery(query);
            return result.ToList();
        }

        public List<string> GetAllPartitions()
        {
            TableQuery<DynamicTableEntity> query = new TableQuery<DynamicTableEntity>().Select(PartitionAndRowKey);
            IEnumerable<DynamicTableEntity> result = postsTable.ExecuteQuery(query);
            return result.Select(x => x.PartitionKey).Distinct().ToList();
        }

        public List<PostEntity> GetMostRecent(string blogName, int maxCount = 50, int offset = 0)
        {
            string pkFilter = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, blogName);
            TableQuery<PostEntity> query = new TableQuery<PostEntity>().Where(pkFilter).Select(FrontendColumns);
            IEnumerable<PostEntity> result = postsTable.ExecuteQuery(query);
            return result.OrderByDescending(x => x.RowKey).Skip(offset).Take(maxCount).ToList();
        }

        public void MarkPhotosAsDownloaded(string blogName, string postId, List<Model.Site.Photo> sitePhotos)
        {
            PhotoDownloadCompleteEntity entity = new PhotoDownloadCompleteEntity
            {
                PartitionKey = blogName,
                RowKey = postId,
                PicsDownloadLevel = Constants.MaxPicsDownloadLevel,
                PhotoBlobUrls = JsonConvert.SerializeObject(sitePhotos)
            };

            TableOperation insertOrMergeOperation = TableOperation.InsertOrMerge(entity);
            postsTable.Execute(insertOrMergeOperation);
        }

        public void MarkVideosAsDownloaded(string blogName, string postId, Video[] videos)
        {
            VideoDownloadCompleteEntity entity = new VideoDownloadCompleteEntity
            {
                PartitionKey = blogName,
                RowKey = postId,
                VideosDownloadLevel = Constants.MaxVideosDownloadLevel,
                VideoBlobUrls = JsonConvert.SerializeObject(videos, JsonSerializerSettings)
            };

            TableOperation insertOrMergeOperation = TableOperation.InsertOrMerge(entity);
            postsTable.Execute(insertOrMergeOperation);
        }

        public void MarkPostNotFound(string blogName, string postId)
        {
            PostNotFoundEntity entity = new PostNotFoundEntity
            {
                PartitionKey = blogName,
                RowKey = postId,
                PostNotFound = true
            };

            TableOperation insertOrMergeOperation = TableOperation.InsertOrMerge(entity);
            postsTable.Execute(insertOrMergeOperation);
        }
    }
}
