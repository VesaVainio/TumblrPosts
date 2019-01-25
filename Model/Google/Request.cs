using System.Collections.Generic;

namespace Model.Google
{
    public class Request
    {
        public static readonly List<Feature> DefaultFeatures = new List<Feature>
        {
            new Feature
            {
                Type = "DOCUMENT_TEXT_DETECTION",
            },
            new Feature
            {
                Type = "LABEL_DETECTION",
                MaxResults = 15
            },
            new Feature
            {
                Type = "WEB_DETECTION",
                MaxResults = 15
            }
        };
        
        public Image Image { get; set; }
        public List<Feature> Features { get; set; }

        public static Request CreateWithDefaultFeatures(string imageUri)
        {
            return new Request
            {
                Image = new Image
                {
                    Source = new ImageSource {ImageUri = imageUri}
                },
                Features = DefaultFeatures
            };
        }
    }
}