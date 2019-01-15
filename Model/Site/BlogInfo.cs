namespace Model.Site
{
    public class BlogInfo
    {
        public string Name { get; set; }

        public string Title { get; set; }
        public long Updated { get; set; }
        public string Description { get; set; }
        public long Posts { get; set; }

        public int Text { get; set; }
        public int Quote { get; set; }
        public int Link { get; set; }
        public int Answer { get; set; }
        public int Video { get; set; }
        public int Audio { get; set; }
        public int Photo { get; set; }
        public int Chat { get; set; }
        public int TotalPosts { get; set; }

        public int Gifs { get; set; }
        public int Jpgs { get; set; }
        public int Pngs { get; set; }
        public int PhotosCount { get; set; }
        public double AverageWidth { get; set; }
    }
}
