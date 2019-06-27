using System;

namespace QueueInterface.Messages
{
    public class PhotoToAnalyze
    {
        public string Url { get; set; }
        public string Blog { get; set; }
        public DateTime PostDate { get; set; }
        public string Error { get; set; } // only for poison messages
        public string StackTrace { get; set; } // only for poison messages
    }
}