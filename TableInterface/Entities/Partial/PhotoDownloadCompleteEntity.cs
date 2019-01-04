using Microsoft.Azure.CosmosDB.Table;

namespace TableInterface.Entities.Partial
{
    public class PhotoDownloadCompleteEntity : TableEntity
    {
        public int PicsDownloadLevel { get; set; }
        public string PhotoBlobUrls { get; set; }
    }
}
