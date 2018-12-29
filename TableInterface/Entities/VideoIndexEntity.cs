﻿using Microsoft.Azure.CosmosDB.Table;
using System;

namespace TableInterface.Entities
{
    public class VideoIndexEntity : TableEntity
    {
        public string Uri { get; set; }
        public string ThumbUri { get; set; }
        public string VideoType { get; set; }
        public double Duration { get; set; }
        public int Bytes { get; set; }
        public string PostId { get; set; }
        public DateTime PostDate { get; set; }

        public VideoIndexEntity(string blogName, string postId, DateTime postDate, int bytes)
        {
            PartitionKey = blogName;
            RowKey = postDate.ToString("yyyyMMddHHmmss") + "-" + postId + "-" + bytes;
            PostId = postId;
            PostDate = postDate;
            Bytes = bytes;
        }
    }
}
