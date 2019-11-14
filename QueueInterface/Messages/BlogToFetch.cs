namespace QueueInterface.Messages
{
    public class BlogToFetch
    {
        public string Blogname { get; set; }
        public long TotalPostCount { get; set; }
        public long? NewerThan { get; set; }
        public bool UpdateNpf { get; set; }
    }
}
