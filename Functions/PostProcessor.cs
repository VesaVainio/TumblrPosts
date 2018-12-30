﻿using HtmlAgilityPack;
using Microsoft.Azure.WebJobs.Host;
using QueueInterface;
using QueueInterface.Messages;
using QueueInterface.Messages.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
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

        public async Task ProcessPosts(IEnumerable<Post> posts, TraceWriter log, string likerBlogname = null)
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

                List<VideoUrls> videoUrlsList = new List<VideoUrls>();

                if (!string.IsNullOrEmpty(post.Video_url))
                {
                    VideoUrls videoUrls = new VideoUrls
                    {
                        VideoUrl = post.Video_url,
                        VideoThumbUrl = post.Thumbnail_url
                    };

                    videoUrlsList.Add(videoUrls);
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
                        List<VideoUrls> videoUrlsListFromBody = GetVideoUrls(htmlDoc, log);
                        videoUrlsList.AddRange(videoUrlsListFromBody);
                    }
                }

                Player lastPlayer = post.Player.LastOrDefault();

                if (lastPlayer != null && post.Video_type.Equals("instagram", StringComparison.OrdinalIgnoreCase))
                {
                    HtmlDocument htmlDoc = new HtmlDocument();
                    htmlDoc.LoadHtml(lastPlayer.Embed_code);
                    HtmlNode blockquoteNode = htmlDoc.DocumentNode.Descendants("blockquote")
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

        private async Task<VideoUrls> GetInstagramVideo(string url)
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
