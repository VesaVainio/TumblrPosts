using System.Collections.Generic;

namespace Model.Tumblr
{
    public class Likes
    {
        public List<Post> Liked_posts { get; set; }
        public int Liked_count { get; set; }
        public Links _links { get; set; }

    }
}
