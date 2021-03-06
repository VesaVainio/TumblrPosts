﻿using Model.Tumblr;
using QueueInterface.Messages.Dto;

namespace QueueInterface.Messages
{
    public class VideosToDownload
    {
        public VideoUrls[] VideoUrls { get; set; }
        public string VideoType { get; set; }
        public double Duration { get; set; }
        public PostIndexInfo IndexInfo { get; set; }
        public string ReblogKey { get; set; }
        public string SourceBlog { get; set; }
        public string PostType { get; set; } // needed to be able to insert ReversePostEntity
        public string Body { get; set; } // needed to be able to insert ReversePostEntity
        public string Title { get; set; } // needed to be able to insert ReversePostEntity

        public VideosToDownload() { }

        public VideosToDownload(Post tumblrPost)
        {
            VideoType = tumblrPost.Video_type;
            Duration = tumblrPost.Duration.HasValue ? tumblrPost.Duration.Value : 0;
            IndexInfo = new PostIndexInfo { BlogName = tumblrPost.Blog_name, PostId = tumblrPost.Id.ToString(), PostDate = tumblrPost.Date };
            ReblogKey = string.IsNullOrEmpty(tumblrPost.Reblog_key) ? null : tumblrPost.Reblog_key;
            SourceBlog = string.IsNullOrEmpty(tumblrPost.Source_title) ? null : tumblrPost.Source_title;
            PostType = tumblrPost.Type.ToString();
            Body = tumblrPost.Body;
            Title = tumblrPost.Title;
        }
    }
}
