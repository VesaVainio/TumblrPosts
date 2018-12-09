using TumblrPics.Model.Tumblr;

namespace QueueInterface.Messages
{
    public class PhotosToDownload
    {
        public Photo[] Photos { get; set; }
        public string[] BlogNames { get; set; }
        public string ReblogKey { get; set; }

        public PhotosToDownload(Post tumblrPost)
        {
            Photos = tumblrPost.Photos;
            BlogNames = tumblrPost.Source_title != null ? new string[] { tumblrPost.Blog_name, tumblrPost.Source_title } : new string[] { tumblrPost.Blog_name, tumblrPost.Source_title };
            ReblogKey = string.IsNullOrEmpty(tumblrPost.Reblog_key) ? null : tumblrPost.Reblog_key;
        }
    }
}
