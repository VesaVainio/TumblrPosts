using System.Collections.Generic;

namespace Model.Microsoft
{
    public class Analysis
    {
        public IList<Category> Categories { get; set; }
        public Adult Adult { get; set; }
        public Color Color { get; set; }
        public ImageType ImageType { get; set; }
        public IList<Tag> Tags { get; set; }
        public Description Description { get; set; }
        public IList<ImageObject> Objects { get; set; }
        public string RequestId { get; set; }
        public Metadata Metadata { get; set; }
    }
}