namespace Model
{
    public static class SanityHelper
    {
        public static string SanitizeSourceBlog(string sourceBlog)
        {
            return sourceBlog?.Replace(' ', '_').Replace('/', '_').Replace("\\", "_").Replace('?', '_').Replace('#', '_');
        }
    }
}