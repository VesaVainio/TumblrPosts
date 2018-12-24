namespace TumblrPics.Model
{
    public static class Constants
    {
        public const int MaxPicsDownloadLevel = 3;
        public const int MaxVideosDownloadLevel = 3;

        public const int MaxPostsToFetch = 3000;

        public const string VideosToDownloadQueueName = "videos-to-download";
        public const string PhotosToDownloadQueueName = "photos-to-download";

        public const string PostsToProcessQueueName = "posts-to-process";

        public const string BlogToFetchQueueName = "blog-to-fetch";
    }
}
