using System;
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

        public static Request CreateWithSourceAndDefaultFeatures(string imageUri)
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

        public static Request CreateWithContentAndDefaultFeatures(byte[] content)
        {
            return new Request
            {
                Image = new Image
                {
                    Content = Convert.ToBase64String(content)
                },
                Features = DefaultFeatures
            };
        }
    }
}