using System;
using Microsoft.Azure.CosmosDB.Table;

namespace TableInterface.Entities
{
    public class MonthIndex : TableEntity
    {
        public const string MonthKeyFormat = "yyyyMM";

        public long FirstPostId { get; set; }
        
        public int MonthsPosts { get; set; }

        public MonthIndex() { }

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

        public Model.Site.MonthIndex GetSiteEntity()
        {
            return new Model.Site.MonthIndex
            {
                FirstPostId = this.FirstPostId,
                MonthsPosts = this.MonthsPosts,
                YearMonth = this.RowKey.Substring(0, 4) + "-" + RowKey.Substring(4, 2)
            };
        }
    }
}