using Microsoft.Azure.CosmosDB.Table;

namespace TableInterface.Entities
{
    public class FrequencyEntity : TableEntity
    {
        public int Count { get; set; }
        public int? AllocatedIndex { get; set; }
        public bool Ignore { get; set; }
    }
}