using System;
using Microsoft.Azure.CosmosDB.Table;
using Model.Tumblr;

namespace TableInterface.Entities
{
    public class BlogEntity : TableEntity
    {
        /* From Tumblr */
        public string Title { get; set; }
        public long Updated { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public long Posts { get; set; }

        /* Proprietary properties */
        public long? LastFetched { get; set; }
        public int? FetchedUntilOffset { get; set; }
        public DateTime? AnalyzedStartingFrom { get; set; }
        public DateTime? AnalyzedUntil { get; set; }

        public BlogEntity() { }

        public BlogEntity(Blog blog) {
            PartitionKey = blog.Name;
            RowKey = "info";

            Title = blog.Title;
            Updated = blog.Updated;
            Name = blog.Name;
            Description = blog.Description;
            Posts = blog.Posts;
        }
    }
}
