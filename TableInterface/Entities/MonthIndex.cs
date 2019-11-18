using System;
using Microsoft.Azure.CosmosDB.Table;

namespace TableInterface.Entities
{
    public class MonthIndex : TableEntity
    {
        public const string MonthKeyFormat = "yyyyMM";

        public long FirstPostId { get; set; }
        
        public int MonthsPosts { get; set; }

        public MonthIndex(string blogname, DateTime month)
        {
            PartitionKey = blogname;
            RowKey = month.ToString(MonthKeyFormat);
        }

        public MonthIndex(string blogname, string monthKey)
        {
            PartitionKey = blogname;
            RowKey = monthKey;
        }
    }
}