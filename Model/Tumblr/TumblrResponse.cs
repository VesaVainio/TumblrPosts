namespace TumblrPics.Model.Tumblr
{
    public class TumblrResponse<T>
    {
        public Meta Meta { get; set; }
        public T Response { get; set; }
    }
}
