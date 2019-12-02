using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.Azure.WebJobs.Host;
using Model.Tumblr;
using QueueInterface;
using QueueInterface.Messages;
using QueueInterface.Messages.Dto;
using TableInterface;
using TableInterface.Entities;
using TumblrPics.Model;
using TumblrPics.Model.Tumblr;

namespace Functions
{
    public class PostProcessor
    {
        public static readonly int[] DownloadSizes = { 1280, 640, 250 };
        private LikeIndexTableAdapter likeIndexTableAdapter;

        private PostsTableAdapter postsTableAdapter;
        private MediaToDownloadQueueAdapter queueAdapter;

        public void Init(TraceWriter log)
        {
            postsTableAdapter = new PostsTableAdapter();
            postsTableAdapter.Init(log);

            likeIndexTableAdapter = new LikeIndexTableAdapter();
            likeIndexTableAdapter.Init();

            queueAdapter = new MediaToDownloadQueueAdapter();
            queueAdapter.Init(log);
        }

        public async Task ProcessPosts(IEnumerable<Post> posts, TraceWriter log, string likerBlogname = null)
        {
            foreach (Post post in posts)
            {
                SanitizePostPhotos(post); // sometimes post.Photos has Alt_sizes with length 0, needs to be sanitized

                PostEntity postEntityInTable = postsTableAdapter.GetPost(post.Blog_name, post.Id.ToString());

                PostEntity postEntityFromTumblr = new PostEntity(post);

                if (!postsTableAdapter.InsertPost(postEntityFromTumblr))
                {
                    break;
                }

                if (likerBlogname != null && post.Liked_Timestamp.HasValue)
                {
                    likeIndexTableAdapter.InsertLikeIndex(likerBlogname, post.Liked_Timestamp.ToString(), post.Blog_name, post.Id.ToString(), post.Reblog_key);
                }

                log.Info("Post " + post.Blog_name + "/" + post.Id + " inserted to table");

                PhotosToDownload photosToDownloadMessage = null;

                if (postEntityFromTumblr.PhotosJson != null)
                {
                    if (postEntityInTable == null || postEntityInTable.PicsDownloadLevel == null ||
                        postEntityInTable.PicsDownloadLevel < Constants.MaxPicsDownloadLevel)
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

                List<VideoUrls> videoUrlsList = new List<VideoUrls>();

                if (post.Content != null && post.Content.Length > 0)
                {
                    List<Photo> photos = new List<Photo>(post.Content.Length);

                    foreach (Content content in post.Content)
                    {
                        if (content.Type == "image")
                        {
                            Photo photo = ConvertContentToPhoto(content);
                            photos.Add(photo);
                        }
                        else if (content.Type == "video" && content.Url != null && content.Poster != null)
                        {
                            VideoUrls videoUrls = new VideoUrls
                            {
                                VideoUrl = content.Url,
                                VideoThumbUrl = content.Poster.OrderBy(x => x.Width).LastOrDefault()?.Url
                            };
                            videoUrlsList.Add(videoUrls);
                        }
                    }

                    if (photos.Count > 0)
                    {
                        UpdatePhotosToDownloadMessage(ref photosToDownloadMessage, post, photos);
                    }
                }

                if (postEntityInTable == null || postEntityInTable.VideosDownloadLevel == null ||
                    postEntityInTable.VideosDownloadLevel < Constants.MaxVideosDownloadLevel)
                {
                    if (!string.IsNullOrEmpty(post.Video_url))
                    {
                        VideoUrls videoUrls = new VideoUrls
                        {
                            VideoUrl = post.Video_url,
                            VideoThumbUrl = post.Thumbnail_url
                        };

                        videoUrlsList.Add(videoUrls);
                    }

                    if (post.Player != null && post.Player.Length > 0 && post.Video_type.Equals("instagram", StringComparison.OrdinalIgnoreCase))
                    {
                        Player largestPlayer = post.Player.OrderBy(x => x.Width).Last();

                        HtmlDocument playerHtmlDoc = new HtmlDocument();
                        playerHtmlDoc.LoadHtml(largestPlayer.Embed_code);
                        HtmlNode blockquoteNode = playerHtmlDoc.DocumentNode.Descendants("blockquote")
                            .FirstOrDefault(x => !string.IsNullOrEmpty(x.Attributes["data-instgrm-permalink"].Value));
                        if (blockquoteNode != null)
                        {
                            string url = blockquoteNode.Attributes["data-instgrm-permalink"].Value;
                            VideoUrls videoUrls = await GetInstagramVideo(url);
                            if (videoUrls != null)
                            {
                                videoUrlsList.Add(videoUrls);
                            }
                        }
                    }
                }

                if (!string.IsNullOrEmpty(post.Body))
                {
                    HtmlDocument htmlDoc = new HtmlDocument();
                    htmlDoc.LoadHtml(post.Body);
                    if (postEntityInTable == null || postEntityInTable.PicsDownloadLevel == null ||
                        postEntityInTable.PicsDownloadLevel < Constants.MaxPicsDownloadLevel)
                    {
                        List<Photo> photos = ExctractPhotosFromHtml(htmlDoc);

                        if (photos.Count > 0)
                        {
                            photosToDownloadMessage = UpdatePhotosToDownloadMessage(ref photosToDownloadMessage, post, photos);
                        }
                    }

                    if (postEntityInTable == null || postEntityInTable.VideosDownloadLevel == null ||
                        postEntityInTable.VideosDownloadLevel < Constants.MaxVideosDownloadLevel)
                    {
                        List<VideoUrls> videoUrlsListFromBody = GetVideoUrls(htmlDoc, log);
                        videoUrlsList.AddRange(videoUrlsListFromBody);
                    }
                }

                if (photosToDownloadMessage != null)
                {
                    queueAdapter.SendPhotosToDownload(photosToDownloadMessage);
                    log.Info("PhotosToDownload message published");
                }

                if (videoUrlsList.Count > 0)
                {
                    VideosToDownload videosToDownload = new VideosToDownload(post)
                    {
                        VideoUrls = videoUrlsList.ToArray()
                    };

                    queueAdapter.SendVideosToDownload(videosToDownload);
                    log.Info("VideosToDownload message published");
                }
            }
        }

        private static Photo ConvertContentToPhoto(Content content)
        {
            return new Photo
            {
                Alt_sizes = (from media in content.Media
                    let helper = PhotoUrlHelper.ParseTumblr(media.Url)
                    where DownloadSizes.Contains(helper.Size) && (helper.VSize == null || helper.VSize > helper.Size)
                    select new AltSize { Url = media.Url, Width = media.Width, Height = media.Height }).ToArray()
            };
        }

        private static PhotosToDownload UpdatePhotosToDownloadMessage(ref PhotosToDownload photosToDownloadMessage, Post post, List<Photo> photos)
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

            return photosToDownloadMessage;
        }

        public static List<Photo> ExctractPhotosFromHtml(HtmlDocument htmlDoc)
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

            return photos;
        }

        private static void SanitizePostPhotos(Post post)
        {
            if (post.Photos != null && post.Photos.Length > 0 && post.Photos.Any(x => x.Alt_sizes.Length == 0))
            {
                post.Photos = post.Photos.Where(x => x.Alt_sizes.Length > 0).ToArray();
            }
        }

        private static async Task<VideoUrls> GetInstagramVideo(string url)
        {
            using (HttpClient httpClient = new HttpClient())
            {
                HttpResponseMessage response = await httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    string content = await response.Content.ReadAsStringAsync();
                    HtmlDocument htmlDoc = new HtmlDocument();
                    htmlDoc.LoadHtml(content);
                    List<HtmlNode> metaNodes = htmlDoc.DocumentNode.Descendants("meta").ToList();
                    VideoUrls videoUrls = new VideoUrls();
                    foreach (HtmlNode node in metaNodes)
                    {
                        if (node.Attributes["property"] != null && node.Attributes["property"].Value.Equals("og:image"))
                        {
                            videoUrls.VideoThumbUrl = node.Attributes["content"].Value;
                        }
                        else if (node.Attributes["property"] != null && node.Attributes["property"].Value.Equals("og:video"))
                        {
                            videoUrls.VideoUrl = node.Attributes["content"].Value;
                        }
                    }

                    if (videoUrls.VideoUrl != null && videoUrls.VideoThumbUrl != null)
                    {
                        return videoUrls;
                    }
                }
            }

            return null;
        }

        public List<VideoUrls> GetVideoUrls(HtmlDocument htmlDocument, TraceWriter log)
        {
            List<HtmlNode> videoNodes = htmlDocument.DocumentNode.Descendants("video").ToList();
            List<VideoUrls> videoUrlsList = new List<VideoUrls>();
            foreach (HtmlNode videoNode in videoNodes)
            {
                VideoUrls videoUrls = new VideoUrls();

                if (videoNode.Attributes["poster"] != null && !string.IsNullOrEmpty(videoNode.Attributes["poster"].Value))
                {
                    videoUrls.VideoThumbUrl = videoNode.Attributes["poster"].Value;
                }

                if (videoNode.Attributes["src"] != null && !string.IsNullOrEmpty(videoNode.Attributes["src"].Value))
                {
                    videoUrls.VideoUrl = videoNode.Attributes["src"].Value;
                }
                else
                {
                    HtmlNode sourceNode = videoNode.Descendants("source").FirstOrDefault();
                    if (sourceNode?.Attributes["src"] != null && !string.IsNullOrEmpty(sourceNode.Attributes["src"].Value))
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

        private static Photo GeneratePhotoFromSrc(string src)
        {
            PhotoUrlHelper helper = PhotoUrlHelper.ParseTumblr(src);
            if (helper == null)
            {
                return null;
            }

            Photo photo = new Photo();
            List<AltSize> altSizes = new List<AltSize>();

            if (!string.IsNullOrEmpty(helper.Hash)) // new format photo url
            {
                altSizes.Add(new AltSize { Url = src });
            }
            else
            {
                foreach (int size in DownloadSizes)
                {
                    AltSize altSize = new AltSize
                    {
                        Url = "https://" + helper.Server + ".media.tumblr.com/" + (helper.Container != null ? helper.Container + "/" : "") + "tumblr_" +
                              helper.Name + "_" + size + "." + helper.Extension,
                        Width = 0,
                        Height = 0
                    };
                    altSizes.Add(altSize);
                }
            }

            photo.Alt_sizes = altSizes.ToArray();

            return photo;
        }
    }
}