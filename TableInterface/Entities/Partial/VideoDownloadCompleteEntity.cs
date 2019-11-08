using Microsoft.Azure.CosmosDB.Table;

namespace TableInterface.Entities.Partial
{
    public class VideoDownloadCompleteEntity : TableEntity
    {
        public int VideosDownloadLevel { get; set; }
        public string VideoBlobUrls { get; set; }
        public string VideoDownloadError { get; set; }
        public string ModifiedBody { get; set; } // same as original body, but with URLs mapped as Blob urls
        // this is a reasonable time to write the ModifiedBody, as the mapped URLs should be known at this time
    }
}
