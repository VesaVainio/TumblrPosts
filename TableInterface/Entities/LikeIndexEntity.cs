using Microsoft.Azure.CosmosDB.Table;

namespace TableInterface.Entities
{
    public class LikeIndexEntity : TableEntity
    {
        public string LikedBlogName { get; set; }
        public string LikedPostId { get; set; }
        public string ReblogKey { get; set; }

        public LikeIndexEntity(string blogName, string likedTimestamp, string likedBlogname, string likedPostId, string reblogKey)
        {
            PartitionKey = blogName;
            RowKey = likedTimestamp + "-" + likedBlogname + "-" + likedPostId;
            LikedBlogName = LikedBlogName;
            LikedPostId = likedPostId;
            ReblogKey = reblogKey;
        }
    }
}
