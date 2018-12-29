using Microsoft.Azure.CosmosDB.Table;

namespace TableInterface.Entities
{
    public class PhotosByBlog : TableEntity
    {
        public int Gifs { get; set; }
        public int Jpgs { get; set; }
        public int Pngs { get; set; }

        public int PhotosCount { get; set; }

        public long TotalWidth { get; set; }
        public int PhotosWithWidthCount { get; set; }
        public double AverageWidth { get; set; }

        //public long TotalBytes { get; set; }
        //public double AverageBytes { get; set; }

        public PhotosByBlog(string blogname)
        {
            PartitionKey = "photos";
            RowKey = blogname;
        }
    }
}
