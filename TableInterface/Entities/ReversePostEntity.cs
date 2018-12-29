using Microsoft.Azure.CosmosDB.Table;
using System;

namespace TableInterface.Entities
{
    public class ReversePostEntity : TableEntity
    {
        public string Blogname { get; set; }
        public string Id { get; set; }
        public string Type { get; set; }
        public DateTime Date { get; set; }
        public string Photos { get; set; } // JSON of Model.Site.Photo[]
        public string Videos { get; set; } // JSON of Model.Site.Video[]

        public ReversePostEntity() { }

        public ReversePostEntity(string blogname, string id, string type, DateTime date)
        {
            PartitionKey = blogname;
            long idLong = long.Parse(id);
            RowKey = (10000000000000 - idLong).ToString();
            Type = type;
            Date = date;
        }
    }
}
