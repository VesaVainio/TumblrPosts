using HtmlAgilityPack;
using Microsoft.Azure.WebJobs.Host;
using QueueInterface;
using QueueInterface.Messages;
using System.Collections.Generic;
using System.Linq;
using TableInterface;
using TableInterface.Entities;
using TumblrPics.Model;
using TumblrPics.Model.Tumblr;

namespace Functions
{
    public class PostProcessor
    {
        private PostsTableAdapter tableAdapter;
        private QueueAdapter queueAdapter;

        public void Init(TraceWriter log)
        {
            log.Info("Starting PostProcessor init");

            tableAdapter = new PostsTableAdapter();
            tableAdapter.Init(log);

            queueAdapter = new QueueAdapter();
            queueAdapter.Init(log);
        }

        public void ProcessPosts(IEnumerable<Post> posts, TraceWriter log)
        {
            foreach (Post post in posts)
            {
                PostEntity postEntityInTable = tableAdapter.GetPost(post.Blog_name, post.Id.ToString());

                PostEntity postEntityFromTumblr = new PostEntity(post);

                tableAdapter.InsertPost(postEntityFromTumblr);
                log.Info("Post " + post.Blog_name + "/" + post.Id + " inserted to table");

                if (postEntityFromTumblr.PhotosCount > 0)
                {
                    if (post.Photos == null)
                    {
                        log.Info("Photos is null!");
                    }

                    if (postEntityInTable == null || postEntityInTable.PicsDownloadLevel < 2)
                    {
                        PhotosToDownload photosToDownloadMessage = new PhotosToDownload(post);

                        queueAdapter.SendPhotosToDownload(photosToDownloadMessage);
                        log.Info("PhotosToDownload message published");
                    }
                    else
                    {
                        log.Info("Photos already downloaded");
                    }
                }

                if (!string.IsNullOrEmpty(post.Body))
                {
                    HtmlDocument htmlDoc = new HtmlDocument();
                    htmlDoc.LoadHtml(post.Body);
                    List<HtmlNode> imgNodes = htmlDoc.DocumentNode.Descendants("img").ToList();
                    List<Photo> photos = new List<Photo>(imgNodes.Count);
                    foreach (HtmlNode imgNode in imgNodes)
                    {
                        string url = imgNode.Attributes["src"].Value;
                        Photo photo = GeneratePhotoFromSrc(url);
                        photos.Add(photo);
                    }
                    if (photos.Count > 0)
                    {
                        Post fakePost = new Post
                        {
                            Blog_name = post.Blog_name,
                            Id = post.Id,
                            Date = post.Date,
                            Reblog_key = post.Reblog_key,
                            Photos = photos.ToArray()
                        };

                        PhotosToDownload photosToDownloadMessage = new PhotosToDownload(fakePost);

                        queueAdapter.SendPhotosToDownload(photosToDownloadMessage);
                        log.Info("PhotosToDownload message published based on IMG tags parsed from body");
                    }
                }
            }
        }

        private Photo GeneratePhotoFromSrc(string src)
        {
            PhotoUrlHelper helper = PhotoUrlHelper.Parse(src);
            if (helper == null)
            {
                return null;
            }

            int[] sizes = { 1280, 640, 500 };
            Photo photo = new Photo();
            List<AltSize> altSizes = new List<AltSize>();

            foreach (int size in sizes)
            {
                AltSize altSize = new AltSize
                {
                    Url = "https://" + helper.Server + ".media.tumblr.com/" + (helper.Container != null ? helper.Container + "/" : "") + "tumblr_" + helper.Name + "_" + size + "." + helper.Extension,
                    Width = 0,
                    Height = 0
                };
                altSizes.Add(altSize);
            }

            photo.Alt_sizes = altSizes.ToArray();

            return photo;
        }
    }
}
