using Microsoft.Azure.CosmosDB.Table;
using System;

namespace TableInterface.Entities
{
    public class PostsByBlogEntity : TableEntity
    {
        public int Photos { get; set; }
        public int Videos { get; set; }
        public int Audios { get; set; }
        public int Chats { get; set; }
        public int Texts { get; set; }
        public int Quotes { get; set; }
        public int Links { get; set; }
        public int Answers { get; set; }

        public int PostsCount { get; set; }

        public long TotalNoteCount { get; set; }

        public DateTime NewestPost { get; set; }
        public DateTime OldestPost { get; set; }

        public PostsByBlogEntity(string blogname)
        {
            PartitionKey = "posts";
            RowKey = blogname;
        }
    }
}
