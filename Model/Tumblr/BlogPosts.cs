using System.Collections.Generic;

namespace TumblrPics.Model.Tumblr
{
    public class BlogPosts
    {
        public Blog Blog { get; set; }
        public List<Post> Posts { get; set; }
    }
}
