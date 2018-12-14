using QueueInterface.Messages.Dto;
using TumblrPics.Model.Tumblr;

namespace QueueInterface.Messages
{
    public class VideoToDownload
    {
        public string VideoUrl { get; set; }
        public string VideoType { get; set; }
        public int Duration { get; set; }
        public PostIndexInfo IndexInfo { get; set; }
        public string ReblogKey { get; set; }

        public VideoToDownload() { }

        public VideoToDownload(Post tumblrPost)
        {
            VideoUrl = tumblrPost.Video_url;
            VideoType = tumblrPost.Video_type;
            Duration = tumblrPost.Duration.HasValue ? tumblrPost.Duration.Value : 0;
            IndexInfo = new PostIndexInfo { BlogName = tumblrPost.Blog_name, PostId = tumblrPost.Id.ToString(), PostDate = tumblrPost.Date };
            ReblogKey = string.IsNullOrEmpty(tumblrPost.Reblog_key) ? null : tumblrPost.Reblog_key;
        }
    }
}
