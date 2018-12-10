using Microsoft.Azure.WebJobs.Host;
using QueueInterface;
using QueueInterface.Messages;
using System.Collections.Generic;
using TableInterface;
using TableInterface.Entities;
using TumblrPics.Model.Tumblr;

namespace Functions
{
    public class PostProcessor
    {
        private PostsTableAdapter tableAdapter;
        private QueueAdapter queueAdapter;

        public void Init()
        {
            tableAdapter = new PostsTableAdapter();
            tableAdapter.Init();

            queueAdapter = new QueueAdapter();
            queueAdapter.Init();
        }

        public void ProcessPosts(IEnumerable<Post> posts, TraceWriter log)
        {
            foreach (Post post in posts)
            {
                PostEntity postEntityInTable = tableAdapter.GetPost(post.Blog_name, post.Id.ToString());

                PostEntity postEntityFromTumblr = new PostEntity(post);

                tableAdapter.InsertPost(postEntityFromTumblr);
                log.Info("Post " + post.Blog_name + "/" + post.Id + " inserted to table");

                if (postEntityFromTumblr.PhotosCount > 0 && (postEntityInTable == null || postEntityInTable.PicsDownloadLevel < 2))
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
        }
    }
}
