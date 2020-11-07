using System.Collections.Generic;

namespace Model.Tumblr
{
    public class BlogPosts
    {
        public Blog Blog { get; set; }
        public List<Post> Posts { get; set; }
        public Links _links { get; set; }
    }
}
