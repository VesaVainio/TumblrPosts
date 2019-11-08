using System.Collections.Generic;
using System.IO;
using System.Linq;
using HtmlAgilityPack;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using TableInterface;
using TableInterface.Entities;
using TumblrPics.Model;

namespace Functions
{
    public static class BodyUrlModifier
    {
        public static string ModifyUrls(string sourceBlog, string body, PhotoIndexTableAdapter photoIndexTableAdapter, TraceWriter log)
        {
            if (string.IsNullOrEmpty(body))
            {
                return null;
            }

            string decodedBody = JsonConvert.DeserializeObject<string>(body);

            HtmlDocument htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(decodedBody);
            List<HtmlNode> imgNodes = htmlDoc.DocumentNode.Descendants("img").ToList();
            foreach (HtmlNode imgNode in imgNodes)
            {
                string url = imgNode.Attributes["src"].Value;

                string mappedUrl = TryToGetMappedUrl(sourceBlog, url, photoIndexTableAdapter);
                if (mappedUrl != null)
                {
                    imgNode.Attributes["src"].Value = mappedUrl;
                }
            }

            StringWriter sw = new StringWriter();
            htmlDoc.Save(sw);
            string result = sw.ToString();
            return result;
        }

        private static string TryToGetMappedUrl(string sourceBlog, string origUrl, PhotoIndexTableAdapter photoIndexTableAdapter)
        {
            PhotoUrlIndexEntity photoIndex = photoIndexTableAdapter.GetPhotoUrlndex(sourceBlog, origUrl);
            if (photoIndex != null)
            {
                return photoIndex.BlobUrl;
            }

            PhotoUrlHelper helper = PhotoUrlHelper.ParseTumblr(origUrl);
            if (helper != null)
            {
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