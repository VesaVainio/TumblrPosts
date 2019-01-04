using Microsoft.Azure.CosmosDB.Table;

namespace TableInterface.Entities.Partial
{
    public class VideoDownloadCompleteEntity : TableEntity
    {
        public int VideosDownloadLevel { get; set; }
        public string VideoBlobUrls { get; set; }
    }
}
