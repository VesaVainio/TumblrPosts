using Microsoft.Azure.CosmosDB.Table;
using Model.Site;
using Newtonsoft.Json;
using System;

namespace TableInterface.Entities
{
    public class ReversePostEntity : TableEntity
    {
        public string Type { get; set; }
        public DateTime Date { get; set; }
        public string Body { get; set; }
        public string Title { get; set; }
        public string Photos { get; set; } // JSON of Model.Site.Photo[]
        public string Videos { get; set; } // JSON of Model.Site.Video[]

        public ReversePostEntity() { }

        public ReversePostEntity(string blogname, string id, string type, DateTime date, string body, string title)
        {
            PartitionKey = blogname;
            long idLong = long.Parse(id);
            RowKey = (10000000000000 - idLong).ToString();
            Type = type;
            Date = date;
            Body = body;
            Title = title;
        }

        public Post GetSitePost()
        {
            return new Post
            {
                Blogname = PartitionKey,
                Id = RowKey,
                Type = Type,
                Date = Date,
                Body = Body,
                Photos = string.IsNullOrEmpty(Photos) ? null : JsonConvert.DeserializeObject<Photo[]>(Photos),
                Videos = string.IsNullOrEmpty(Videos) ? null : JsonConvert.DeserializeObject<Video[]>(Videos),
            };
        }
    }
}
