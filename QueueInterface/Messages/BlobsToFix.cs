using System.Collections.Generic;

namespace QueueInterface.Messages
{
    public class BlobsToFix
    {
        public string Container { get; set; }
        public List<string> BlobNames { get; set; }
    }
}