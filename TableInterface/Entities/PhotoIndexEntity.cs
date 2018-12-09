using Microsoft.Azure.CosmosDB.Table;
using System;

namespace TableInterface.Entities
{
    public class PhotoIndexEntity : TableEntity
    {
        public string Uri { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public string PostId { get; set; }
        public DateTime PostDate { get; set; }

        public PhotoIndexEntity(string blogName, string postId, DateTime postDate)
        {
            PartitionKey = blogName;
            RowKey = postDate.ToString("yyyyMMddHHmmss-" + postId);
            PostId = postId;
            PostDate = postDate;
        }
    }
}
