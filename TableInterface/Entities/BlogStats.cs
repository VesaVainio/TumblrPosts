using Microsoft.Azure.CosmosDB.Table;
using Model.Site;

namespace TableInterface.Entities
{
    public class BlogStats : TableEntity
    {
        public const string BlogStatsPartitionKey = "stats";
        
        // from BlobEntity
        public string Title { get; set; }
        public long Updated { get; set; }
        public string Description { get; set; }
        public long Posts { get; set; }

        // from posts
        public int Text { get; set; }
        public int Quote { get; set; }
        public int Link { get; set; }
        public int Answer { get; set; }
        public int Video { get; set; }
        public int Audio { get; set; }
        public int Photo { get; set; }
        public int Chat { get; set; }

        public int TotalPosts { get; set; }

        // from photos
        public int Gifs { get; set; }
        public int Jpgs { get; set; }
        public int Pngs { get; set; }

        public int PhotosCount { get; set; }

        public long TotalWidth { get; set; }
        public int PhotosWithWidthCount { get; set; }
        public double AverageWidth { get; set; }

        public BlogStats() { }

        public BlogStats(string blogname)
        {
            PartitionKey = BlogStatsPartitionKey;
            RowKey = blogname;
        }

        public void UpdateFromBlogEntity(BlogEntity entity)
        {
            if (entity == null)
            {
                return;
            }

            Title = entity.Title;
            Updated = entity.Updated;
            Description = entity.Description;
            Posts = entity.Posts;
        }

        public BlogInfo GetSiteBlog()
        {
            return new BlogInfo
            {
                Name = RowKey,

                Title = Title,
                Updated = Updated,
                Description = Description,
                Posts = Posts,

                Text = Text,
                Quote = Quote,
                Link = Link,
                Answer = Answer,
                Video = Video,
                Audio = Audio,
                Photo = Photo,
                Chat = Chat,
                TotalPosts = TotalPosts,

                Gifs = Gifs,
                Jpgs = Jpgs,
                Pngs = Pngs,
                PhotosCount = PhotosCount,
                AverageWidth = AverageWidth,
            };  
        }
    }
}
