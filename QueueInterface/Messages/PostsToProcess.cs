using Model.Tumblr;

namespace QueueInterface.Messages
{
    public class PostsToProcess
    {
        public Post[] Posts { get; set; }
        public string LikerBlogname { get; set; }
    }
}
