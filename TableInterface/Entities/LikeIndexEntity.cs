using Microsoft.Azure.CosmosDB.Table;

namespace TableInterface.Entities
{
    public class LikeIndexEntity : TableEntity
    {
        public string LikedBlogName { get; set; }
        public string LikedPostId { get; set; }
        public string LikedTimestamp { get; set; }
        public string ReblogKey { get; set; }

        public LikeIndexEntity() { }

        public LikeIndexEntity(string blogName, string likedTimestamp, string likedBlogname, string likedPostId, string reblogKey)
        {
            PartitionKey = blogName;
            RowKey = likedTimestamp + "-" + likedBlogname + "-" + likedPostId;
            LikedBlogName = likedBlogname;
            LikedPostId = likedPostId;
            LikedTimestamp = likedTimestamp;
            ReblogKey = reblogKey;
        }
    }
}
