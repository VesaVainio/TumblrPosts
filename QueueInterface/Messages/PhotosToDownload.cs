﻿using QueueInterface.Messages.Dto;
using TumblrPics.Model.Tumblr;

namespace QueueInterface.Messages
{
    public class PhotosToDownload
    {
        public Photo[] Photos { get; set; }
        public PostIndexInfo IndexInfo { get; set; }
        public string ReblogKey { get; set; }
        public string SourceBlog { get; set; }
        public string PostType { get; set; } // needed to be able to insert ReversePostEntity
        public string Body { get; set; } // needed to be able to insert ReversePostEntity
        public string Title { get; set; } // needed to be able to insert ReversePostEntity

        public PhotosToDownload() { }

        public PhotosToDownload(Post tumblrPost)
        {
            IndexInfo = new PostIndexInfo { BlogName = tumblrPost.Blog_name, PostId = tumblrPost.Id.ToString(), PostDate = tumblrPost.Date };
            ReblogKey = string.IsNullOrEmpty(tumblrPost.Reblog_key) ? null : tumblrPost.Reblog_key;
            SourceBlog = string.IsNullOrEmpty(tumblrPost.Source_title) ? null : tumblrPost.Source_title;
            PostType = tumblrPost.Type.ToString();
            Body = tumblrPost.Body;
            Title = tumblrPost.Title;
        }
    }
}
