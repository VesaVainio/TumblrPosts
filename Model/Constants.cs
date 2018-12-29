namespace TumblrPics.Model
{
    public static class Constants
    {
        public const int MaxPicsDownloadLevel = 4;
        public const int MaxVideosDownloadLevel = 4;

        public const int MaxPostsToFetch = 3000;

        public const string VideosToDownloadQueueName = "videos-to-download";
        public const string PhotosToDownloadQueueName = "photos-to-download";
        public const string PostsToProcessQueueName = "posts-to-process";
        public const string BlogToFetchQueueName = "blog-to-fetch";
        public const string BlogToIndexQueueName = "blog-to-index";
        public const string PostToGetQueueName = "post-to-get";
    }
}
