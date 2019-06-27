using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;
using QueueInterface;
using QueueInterface.Messages;
using System;
using System.Threading.Tasks;
using TableInterface;
using TableInterface.Entities;

namespace Functions
{
    public static class ProcessPhotoToAnalyze
    {
        [FunctionName("ProcessPhotoToAnalyze")]
        public static async Task Run([TimerTrigger("0 15,45 * * * *")]TimerInfo myTimer, TraceWriter log)
        {
            Startup.Init();

            PhotoToAnalyzeQueueAdapter photoToAnalyzeQueueAdapter = new PhotoToAnalyzeQueueAdapter();
            photoToAnalyzeQueueAdapter.Init();
            CloudQueueMessage message = null;
            PhotoToAnalyze photoToAnalyze = null;

            try
            {
                ImageAnalysisTableAdapter imageAnalysisTableAdapter = new ImageAnalysisTableAdapter();
                imageAnalysisTableAdapter.Init();

                BlogInfoTableAdapter blogInfoTableAdapter = new BlogInfoTableAdapter();
                blogInfoTableAdapter.Init();

                int processedCount = 0;

                do
                {
                    message = await photoToAnalyzeQueueAdapter.GetNextMessage();
                    if (message == null)
                    {
                        return;
                    }

                    photoToAnalyze = JsonConvert.DeserializeObject<PhotoToAnalyze>(message.AsString);

                    if (imageAnalysisTableAdapter.GetImageAnalysis(photoToAnalyze.Url) != null)
                    {
                        log.Info($"Image {photoToAnalyze.Url} already analyzed, aborting");
                        await UpdateBlogInfo(photoToAnalyze, blogInfoTableAdapter);
                        continue;
                    }

                    string failure = await ImageAnalyzer.AnalyzePhoto(log, photoToAnalyze, imageAnalysisTableAdapter);
                    if (string.IsNullOrEmpty(failure))
                    {
                        await UpdateBlogInfo(photoToAnalyze, blogInfoTableAdapter);
                    }
                    else
                    {
                        photoToAnalyze.Error = failure;
                        await photoToAnalyzeQueueAdapter.SendToPoisonQueue(photoToAnalyze);
                        log.Info($"Message for {photoToAnalyze.Url} stored to poison queue");
                    }

                    processedCount++;

                    await photoToAnalyzeQueueAdapter.DeleteMessage(message);
                } while (processedCount < 10);
            }
            catch (Exception ex)
            {
                log.Error("Error in ProcessPhotosToAnalyze: " + ex.Message, ex);

                if (message != null && photoToAnalyze != null)
                {
                    photoToAnalyze.Error = ex.Message;
                    photoToAnalyze.StackTrace = ex.StackTrace.Substring(0, 40000);
                    await photoToAnalyzeQueueAdapter.SendToPoisonQueue(photoToAnalyze);
                    log.Info($"Message for {photoToAnalyze.Url} stored to poison queue");
                    await photoToAnalyzeQueueAdapter.DeleteMessage(message);
                    log.Info($"Failed message deleted successfully from main queue");
                    message = null;
                }
            }
        }

        private static async Task UpdateBlogInfo(PhotoToAnalyze photoToAnalyze, BlogInfoTableAdapter tableAdapter)
        {
            BlogEntity info = await tableAdapter.GetBlog(photoToAnalyze.Blog);

            bool changed = false;

            if (info.AnalyzedStartingFrom == null || info.AnalyzedStartingFrom > photoToAnalyze.PostDate)
            {
                info.AnalyzedStartingFrom = photoToAnalyze.PostDate;
                changed = true;
            }

            if (info.AnalyzedUntil == null || info.AnalyzedUntil < photoToAnalyze.PostDate)
            {
                info.AnalyzedUntil = photoToAnalyze.PostDate;
                changed = true;
            }

            if (changed)
            {
                tableAdapter.InsertBlog(info);
            }
        }
    }
}