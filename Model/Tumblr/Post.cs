using System;
using Model.Tumblr;

namespace TumblrPics.Model.Tumblr
{
    public class Post
    {
        public long Id { get; set; }
        public string Post_url { get; set; }
        public string Blog_name { get; set; }

        public PostType Type { get; set; }

        public long Timestamp { get; set; }
        public long? Liked_Timestamp { get; set; }
        public DateTime Date { get; set; }
        public string[] Tags { get; set; }
        public string Source_title { get; set; }
        public string Source_url { get; set; }
        public int Note_count { get; set; }
        public string Reblog_key { get; set; }
        //public Reblog Reblog { get; set; }
        public Trail[] Trail { get; set; }

        public bool ShouldOpenInLegacy { get; set; }
        public Content[] Content { get; set; }


        public string Title { get; set; }
        public string Body { get; set; }
        public string Format { get; set; }

        //public string Caption { get; set; }
        public int Width { get; set; }
        public int Heigth { get; set; }

        public Photo[] Photos { get; set; }

        public string Video_type { get; set; }
        public string Video_url { get; set; }
        public string Thumbnail_url { get; set; }
        public double? Duration { get; set; }
        public Player[] Player { get; set; }
    }
}