using Microsoft.Azure.CosmosDB.Table;

namespace TableInterface.Entities
{
    public class ImageAnalysisEntity : TableEntity
    {
        public string CanonicalJson { get; set; }
        public string GoogleVisionApiJson { get; set; }
        public string MsCognitiveFaceDetectJson { get; set; }
    }
}