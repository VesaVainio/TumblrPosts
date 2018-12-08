using Newtonsoft.Json;
using System;
using System.ComponentModel;

namespace TumblrPics.Model.Tumblr
{
    public class Post
    {
        public long Id { get; set; }
        public string Post_url { get; set; }

  //      [JsonConverter(typeof(EnumConverter))]
		//[JsonProperty(PropertyName = "type")]
        public PostType Type { get; set; }

        public long TimeStamp { get; set; }
        public DateTime Date { get; set; }
        public string[] Tags { get; set; }
        public string Source_url { get; set; }
        public int Note_count { get; set; }

        public string Title { get; set; }
        public string Body { get; set; }

        public Photo[] Photos { get; set; }
    }
}