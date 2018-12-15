using Microsoft.Azure.CosmosDB.Table;

namespace TableInterface.Entities
{
    public class PhotoUrlIndexEntity : TableEntity
    {
        public string BlobUrl { get; set; }
    }
}
