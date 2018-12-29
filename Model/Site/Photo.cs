namespace Model.Site
{
    public class Photo
    {
        public string Name { get; set; }
        public string Extension { get; set; }

        public PhotoSize[] Sizes { get; set; }
    }
}
