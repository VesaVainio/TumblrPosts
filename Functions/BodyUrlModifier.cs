using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using HtmlAgilityPack;
using Microsoft.Azure.WebJobs.Host;
using Model.Site;
using Newtonsoft.Json;
using TableInterface;
using TableInterface.Entities;
using TumblrPics.Model;

namespace Functions
{
    public static class BodyUrlModifier
    {
        public static string ModifyUrls(string sourceBlog, string body, PhotoIndexTableAdapter photoIndexTableAdapter, List<Photo> sitePhotos, out List<TumblrPics.Model.Tumblr.Photo> extractedPhotos)
        {
            extractedPhotos = null;
            
            if (string.IsNullOrEmpty(body))
            {
                return null;
            }

            string decodedBody = body;

            if (body.StartsWith("\""))
            {
                decodedBody = JsonConvert.DeserializeObject<string>(body);
            }

            HtmlDocument htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(decodedBody);
            List<HtmlNode> imgNodes = htmlDoc.DocumentNode.Descendants("img").ToList();
            bool hasPhotosNotFound = false;
            foreach (HtmlNode imgNode in imgNodes)
            {
                string url = imgNode.Attributes["src"].Value;

                string mappedUrl = TryToGetMappedUrl(url, sitePhotos, sourceBlog, photoIndexTableAdapter);
                if (mappedUrl != null)
                {
                    imgNode.Attributes["src"].Value = mappedUrl;
                }
                else
                {
                    hasPhotosNotFound = true;
                }
            }

            if (hasPhotosNotFound)
            {
                extractedPhotos = PostProcessor.ExctractPhotosFromHtml(htmlDoc);
            }

            StringWriter sw = new StringWriter();
            htmlDoc.Save(sw);
            string result = sw.ToString();
            return result;
        }

        private static string TryToGetMappedUrl(string origUrl, List<Photo> sitePhotos, string sourceBlog, PhotoIndexTableAdapter photoIndexTableAdapter)
        {
            PhotoUrlHelper helper = PhotoUrlHelper.ParseTumblr(origUrl);
            if (helper != null)
            {
                if (sitePhotos != null)
                {
                    foreach (Photo sitePhoto in sitePhotos)
                    {
                        int underscoreIndex = sitePhoto.Name.IndexOf("_", StringComparison.Ordinal);
                        if (underscoreIndex >= 0)
                        {
                            string containerPart = sitePhoto.Name.Substring(0, underscoreIndex);
                            string namePart = sitePhoto.Name.Substring(underscoreIndex + 1, sitePhoto.Name.Length - underscoreIndex - 1);

                            if (namePart.Equals(helper.Name) && containerPart.Equals(helper.Container))
                            {
                                PhotoSize photoSize = sitePhoto.Sizes.OrderBy(x => x.Nominal).Last();
                                string blobBaseUrl = ConfigurationManager.AppSettings["BlobBaseUrl"];
                                string newUrl = blobBaseUrl + "/" + photoSize.Container + "/" + sitePhoto.Name + "_" + photoSize.Nominal + "." +
                                                sitePhoto.Extension;
                                return newUrl;
                            }
                        }
                    }
                }

                PhotoUrlIndexEntity photoIndex = photoIndexTableAdapter.GetPhotoUrlndex(sourceBlog, origUrl);
                if (photoIndex != null)
                {
                    return photoIndex.BlobUrl;
                }

                string url = "https://" + helper.Server + ".media.tumblr.com/" + (helper.Container != null ? helper.Container + "/" : "") + "tumblr_" +
                             helper.Name + "_" + 640 + "." + helper.Extension;

                photoIndex = photoIndexTableAdapter.GetPhotoUrlndex(sourceBlog, url);
                if (photoIndex != null)
                {
                    return photoIndex.BlobUrl;
                }
            }

            return null;
        }
    }
}