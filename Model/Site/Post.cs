using System;

namespace Model.Site
{
    public class Post
    {
        public string Blogname { get; set; }
        public string Id { get; set; }
        public string Type { get; set; }
        public DateTime Date { get; set; }
        public Photo[] Photos { get; set; }
    }
}
