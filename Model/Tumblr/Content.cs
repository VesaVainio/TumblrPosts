using Model.Converters;
using Newtonsoft.Json;

namespace Model.Tumblr
{
    public class Content
    {
        public string Type { get; set; }
        public string SubType { get; set; }
        public string Text { get; set; }
        public Formatting[] Formatting { get; set; }

        [JsonConverter(typeof(SingleOrArrayConverter<Media>))]
        public Media[] Media { get; set; }

        public Poster[] Poster { get; set; }
        public string Url { get; set; }
    }
}