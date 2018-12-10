using Microsoft.Azure.CosmosDB.Table;

namespace TableInterface.Entities
{
    public class DownloadCompleteEntity : TableEntity
    {
        public int PicsDownloadLevel { get; set; }
    }
}
