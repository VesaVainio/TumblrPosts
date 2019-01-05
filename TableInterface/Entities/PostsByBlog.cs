using Microsoft.Azure.CosmosDB.Table;

namespace TableInterface.Entities
{
    public class PostsByBlog : TableEntity
    {
        public int Text { get; set; }
        public int Quote { get; set; }
        public int Link { get; set; }
        public int Answer { get; set; }
        public int Video { get; set; }
        public int Audio { get; set; }
        public int Photo { get; set; }
        public int Chat { get; set; }

        public int TotalPosts { get; set; }

        public PostsByBlog(string blogname)
        {
            PartitionKey = "posts";
            RowKey = blogname;
        }
    }
}
