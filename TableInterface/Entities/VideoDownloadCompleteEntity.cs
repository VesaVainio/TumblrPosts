using Microsoft.Azure.CosmosDB.Table;

namespace TableInterface.Entities
{
    public class VideoDownloadCompleteEntity : TableEntity
    {
        public int VideosDownloadLevel { get; set; }
        public string VideoBlobUrls { get; set; }
    }
}
