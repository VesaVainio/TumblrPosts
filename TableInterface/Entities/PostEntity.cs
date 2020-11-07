using Microsoft.Azure.CosmosDB.Table;
using Newtonsoft.Json;
using System;
using Model.Tumblr;

namespace TableInterface.Entities
{
    public class PostEntity : TableEntity
    {
        public string PostUrl { get; set; }

        public string Type { get; set; }

        public long TumblrTimestamp { get; set; }
        public DateTime Date { get; set; }
        public string Tags { get; set; }
        public string SourceTitle { get; set; }
        public string SourceUrl { get; set; }
        public int NoteCount { get; set; }
        public string ReblogKey { get; set; }
        public string Reblog { get; set; }
        public string Trail { get; set; }

        public string Title { get; set; }
        public string Body { get; set; }
        public string ModifiedBody { get; set; } // same as original body, but with URLs mapped as Blob urls

        public string Format { get; set; }

        public string Caption { get; set; }
        public int? Width { get; set; }
        public int? Heigth { get; set; }

        public bool? ShouldOpenInLegacy { get; set; } // NPF format field
        public string Content { get; set; } // NPF format field

        public string PhotoOriginalUrls { get; set; }
        public string PhotosJson { get; set; }
        public string PhotoBlobUrls { get; set; } // separated by ;, only the originals

        public string VideoUrl { get; set; }
        public string VideoOriginalUrl { get; set; }
        public string VideoType { get; set; }
        public string VideoBlobUrls { get; set; } // populated when the videos have been downloaded
        public string VideoThumbBlobUrls { get; set; } // populated when the videos have been downloaded

        public int? PicsDownloadLevel { get; set; }
        public int? VideosDownloadLevel { get; set; }
        public bool PostNotFound { get; set; } // set to true if trying to get the post from Tumblr API and get a 404
        public string VideoDownloadError { get; set; }

        public PostEntity() { }

        public PostEntity(Post tumblrPost)
        {
            PartitionKey = tumblrPost.Blog_name;
            RowKey = tumblrPost.Id.ToString();

            PostUrl = tumblrPost.Post_url;
            Type = tumblrPost.Type.ToString();
            TumblrTimestamp = tumblrPost.Timestamp;
            Date = tumblrPost.Date;
            Tags = tumblrPost.Tags == null || tumblrPost.Tags.Length == 0 ? null : string.Join(";", tumblrPost.Tags);
            SourceTitle = string.IsNullOrEmpty(tumblrPost.Source_title) ? null : tumblrPost.Source_title;
            SourceUrl = string.IsNullOrEmpty(tumblrPost.Source_url) ? null : tumblrPost.Source_url;
            NoteCount = tumblrPost.Note_count;
            ReblogKey = string.IsNullOrEmpty(tumblrPost.Reblog_key) ? null : tumblrPost.Reblog_key;
            Trail = tumblrPost.Trail == null || tumblrPost.Trail.Length == 0 ? null : JsonConvert.SerializeObject(tumblrPost.Trail);

            ShouldOpenInLegacy = tumblrPost.ShouldOpenInLegacy;
            Content = tumblrPost.Content == null || tumblrPost.Content.Length == 0 ? null : JsonConvert.SerializeObject(tumblrPost.Content); 

            Title = tumblrPost.Title;
            Format = tumblrPost.Format;
            Body = string.IsNullOrEmpty(tumblrPost.Body) ? null : JsonConvert.ToString(tumblrPost.Body);

            Width = tumblrPost.Width > 0 ? tumblrPost.Width : (int?)null;
            Heigth = tumblrPost.Heigth > 0 ? tumblrPost.Heigth : (int?)null;

            PhotoOriginalUrls = null;
            PhotosJson = tumblrPost.Photos == null || tumblrPost.Photos.Length == 0 ? null : JsonConvert.SerializeObject(tumblrPost.Photos);

            VideoUrl = tumblrPost.Video_url;
            VideoType = tumblrPost.Video_type;
        }
    }
}
