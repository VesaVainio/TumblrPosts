using QueueInterface.Messages.Dto;
using System.Text.RegularExpressions;
using TumblrPics.Model.Tumblr;

namespace QueueInterface.Messages
{
    public class PhotosToDownload
    {
        //private static Regex sourceUrlRegex = new Regex(@"^https://[0-9a-z\.]+/post/(?<id>[0-9]+)");

        public Photo[] Photos { get; set; }
        public PostIndexInfo IndexInfo { get; set; }
        public string ReblogKey { get; set; }

        public PhotosToDownload() { }

        public PhotosToDownload(Post tumblrPost)
        {
            Photos = tumblrPost.Photos;
            IndexInfo = new PostIndexInfo { BlogName = tumblrPost.Blog_name, PostId = tumblrPost.Id.ToString(), PostDate = tumblrPost.Date };
            ReblogKey = string.IsNullOrEmpty(tumblrPost.Reblog_key) ? null : tumblrPost.Reblog_key;
        }
    }
}
