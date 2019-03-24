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
    public static class ProcessPhotosToAnalyze
    {
        [FunctionName("ProcessPhotosToAnalyze")]
        public static async Task Run([TimerTrigger("0 * * * * *")]TimerInfo myTimer, TraceWriter log)
        {
            Startup.Init();

            try
            {
                PhotoToAnalyzeQueueAdapter photoToAnalyzeQueueAdapter = new PhotoToAnalyzeQueueAdapter();
                photoToAnalyzeQueueAdapter.Init();

                ImageAnalysisTableAdapter imageAnalysisTableAdapter = new ImageAnalysisTableAdapter();
                imageAnalysisTableAdapter.Init();

                BlogInfoTableAdapter blogInfoTableAdapter = new BlogInfoTableAdapter();
                blogInfoTableAdapter.Init();

                int processedCount = 0;

                do
                {
                    CloudQueueMessage message = await photoToAnalyzeQueueAdapter.GetNextMessage();
                    if (message == null)
                    {
                        return;
                    }

                    PhotoToAnalyze photoToAnalyze = JsonConvert.DeserializeObject<PhotoToAnalyze>(message.AsString);

                    if (imageAnalysisTableAdapter.GetImageAnalysis(photoToAnalyze.Url) != null)
                    {
                        log.Info($"Image {photoToAnalyze.Url} already analyzed, aborting");
                        await UpdateBlogInfo(photoToAnalyze, blogInfoTableAdapter);
                        continue;
                    }

                    await ImageAnalyzer.AnalyzePhoto(log, photoToAnalyze, imageAnalysisTableAdapter);

                    await UpdateBlogInfo(photoToAnalyze, blogInfoTableAdapter);

                    processedCount++;

                    await photoToAnalyzeQueueAdapter.DeleteMessage(message);
                } while (processedCount < 5);
            }
            catch (Exception ex)
            {
                log.Error("Error in ProcessPhotosToAnalyze", ex);
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