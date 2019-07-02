using System.Collections.Generic;
using System.Linq;

namespace Model.Google
{
    public class VisionApiRequest
    {
        public List<Request> Requests { get; set; }

        public static VisionApiRequest CreateFromImageUris(params string[] imageUris)
        {
            return new VisionApiRequest
            {
                Requests = imageUris.Select(Request.CreateWithSourceAndDefaultFeatures).ToList()
            };
        }

        public static VisionApiRequest CreateFromContent(byte[] imageContent)
        {
            return new VisionApiRequest
            {
                Requests = new List<Request> { Request.CreateWithContentAndDefaultFeatures(imageContent) }
            };
        }
    }
}