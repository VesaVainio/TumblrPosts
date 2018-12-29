using HtmlAgilityPack;
using Microsoft.Azure.WebJobs.Host;
using QueueInterface;
using QueueInterface.Messages;
using QueueInterface.Messages.Dto;
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
        private PostsTableAdapter postsTableAdapter;
        private LikeIndexTableAdapter LikeIndexTableAdapter;
        private MediaToDownloadQueueAdapter queueAdapter;

        public void Init(TraceWriter log)
        {
            postsTableAdapter = new PostsTableAdapter();
            postsTableAdapter.Init(log);

            LikeIndexTableAdapter = new LikeIndexTableAdapter();
            LikeIndexTableAdapter.Init();

            queueAdapter = new MediaToDownloadQueueAdapter();
            queueAdapter.Init(log);
        }

        public void ProcessPosts(IEnumerable<Post> posts, TraceWriter log, string likerBlogname = null)
        {
            foreach (Post post in posts)
            {
                PostEntity postEntityInTable = postsTableAdapter.GetPost(post.Blog_name, post.Id.ToString());

                PostEntity postEntityFromTumblr = new PostEntity(post);

                if (!postsTableAdapter.InsertPost(postEntityFromTumblr))
                {
                    break;
                }

                if (likerBlogname != null && post.Liked_Timestamp.HasValue)
                {
                    LikeIndexTableAdapter.InsertLikeIndex(likerBlogname, post.Liked_Timestamp.ToString(), post.Blog_name, post.Id.ToString(), post.Reblog_key);
                }

                log.Info("Post " + post.Blog_name + "/" + post.Id + " inserted to table");

                PhotosToDownload photosToDownloadMessage = null;
                VideosToDownload videosToDownload = null;

                if (postEntityFromTumblr.PhotosJson != null)
                {
                    if (postEntityInTable == null || postEntityInTable.PicsDownloadLevel < Constants.MaxPicsDownloadLevel)
                    {
                        photosToDownloadMessage = new PhotosToDownload(post)
                        {
                            Photos = post.Photos
                        };
                    }
                    else
                    {
                        log.Info("Photos already downloaded");
                    }
                }

                if (!string.IsNullOrEmpty(post.Video_url))
                {
                    VideoUrls videoUrls = new VideoUrls();
                    videoUrls.VideoUrl = post.Video_url;
                    videoUrls.VideoThumbUrl = post.Thumbnail_url;

                    videosToDownload = new VideosToDownload(post)
                    {
                        VideoUrls = new[] { videoUrls }
                    };
                }

                if (!string.IsNullOrEmpty(post.Body))
                {
                    HtmlDocument htmlDoc = new HtmlDocument();
                    htmlDoc.LoadHtml(post.Body);
                    if (postEntityInTable == null || postEntityInTable.PicsDownloadLevel < Constants.MaxPicsDownloadLevel)
                    {
                        List<HtmlNode> imgNodes = htmlDoc.DocumentNode.Descendants("img").ToList();
                        List<Photo> photos = new List<Photo>(imgNodes.Count);
                        foreach (HtmlNode imgNode in imgNodes)
                        {
                            string url = imgNode.Attributes["src"].Value;
                            Photo photo = GeneratePhotoFromSrc(url);
                            if (photo != null)
                            {
                                photos.Add(photo);
                            }
                        }
                        if (photos.Count > 0)
                        {
                            if (photosToDownloadMessage == null)
                            {
                                photosToDownloadMessage = new PhotosToDownload(post)
                                {
                                    Photos = photos.ToArray()
                                };
                            }
                            else
                            {
                                photosToDownloadMessage.Photos = photosToDownloadMessage.Photos.Concat(photos).ToArray();
                            }
                        }
                    }

                    if (postEntityInTable == null || postEntityInTable.VideosDownloadLevel < Constants.MaxVideosDownloadLevel)
                    {
                        List<VideoUrls> videoUrlsList = GetVideoUrls(htmlDoc, log);

                        if (videoUrlsList.Count > 0)
                        {
                            if (videosToDownload == null)
                            {
                                videosToDownload = new VideosToDownload(post)
                                {
                                    VideoUrls = videoUrlsList.ToArray()
                                };
                            }
                            else
                            {
                                videosToDownload.VideoUrls = videosToDownload.VideoUrls.Concat(videoUrlsList).ToArray();
                            }
                        }
                    }
                }

                if (photosToDownloadMessage != null)
                {
                    queueAdapter.SendPhotosToDownload(photosToDownloadMessage);
                    log.Info("PhotosToDownload message published");
                }

                if (videosToDownload != null)
                {
                    queueAdapter.SendVideosToDownload(videosToDownload);
                    log.Info("VideosToDownload message published");
                }
            }
        }

        public List<VideoUrls> GetVideoUrls(HtmlDocument htmlDocument, TraceWriter log)
        {
            List<HtmlNode> videoNodes = htmlDocument.DocumentNode.Descendants("video").ToList();
            List<VideoUrls> videoUrlsList = new List<VideoUrls>();
            foreach (HtmlNode videoNode in videoNodes)
            {
                VideoUrls videoUrls = new VideoUrls();

                if (!string.IsNullOrEmpty(videoNode.Attributes["poster"].Value))
                {
                    videoUrls.VideoThumbUrl = videoNode.Attributes["poster"].Value;
                }

                if (!string.IsNullOrEmpty(videoNode.Attributes["src"].Value))
                {
                    videoUrls.VideoUrl = videoNode.Attributes["src"].Value;
                }
                else
                {
                    HtmlNode sourceNode = videoNode.Descendants("source").FirstOrDefault();
                    if (sourceNode != null)
                    {
                        videoUrls.VideoUrl = sourceNode.Attributes["src"].Value;
                    }

                }

                if (videoUrls.VideoUrl != null && videoUrls.VideoThumbUrl != null)
                {
                    videoUrlsList.Add(videoUrls);
                }
                else
                {
                    if (videoUrls.VideoUrl == null)
                    {
                        log.Warning("Missing video url for video thumb url: " + videoUrls.VideoThumbUrl);
                    }
                    else
                    {
                        log.Warning("Missing thumb url for video url: " + videoUrls.VideoUrl);
                    }
                }
            }

            return videoUrlsList;
        }

        private Photo GeneratePhotoFromSrc(string src)
        {
            PhotoUrlHelper helper = PhotoUrlHelper.ParseTumblr(src);
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
