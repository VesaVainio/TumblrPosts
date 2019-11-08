using Microsoft.Azure.CosmosDB.Table;

namespace TableInterface.Entities.Partial
{
    public class PhotoDownloadCompleteEntity : TableEntity
    {
        public int PicsDownloadLevel { get; set; }
        public string PhotoBlobUrls { get; set; }
        public string ModifiedBody { get; set; } // same as original body, but with URLs mapped as Blob urls
        // this is a reasonable time to write the ModifiedBody, as the mapped URLs should be known at this time
    }
}
