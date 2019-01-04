using Microsoft.Azure.CosmosDB.Table;

namespace TableInterface.Entities.Partial
{
    public class PostNotFoundEntity : TableEntity
    {
        public bool PostNotFound { get; set; } 
    }
}
