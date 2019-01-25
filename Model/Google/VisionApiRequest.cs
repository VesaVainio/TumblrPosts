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
                Requests = imageUris.Select(Request.CreateWithDefaultFeatures).ToList()
            };
        }
    }
}