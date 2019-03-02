using System.Collections.Generic;

namespace Model.Google
{
    public class Response
    {
        public IList<LabelAnnotation> LabelAnnotations { get; set; }
        public IList<TextAnnotation> TextAnnotations { get; set; }
        public FullTextAnnotation FullTextAnnotation { get; set; }
        public WebDetection WebDetection { get; set; }
    }
}