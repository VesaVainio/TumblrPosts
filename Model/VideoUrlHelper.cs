using System.Text.RegularExpressions;

namespace TumblrPics.Model
{
    public class VideoUrlHelper
    {
        private static Regex videoUrlRegex = new Regex(@"^https://(?<server>[a-zA-Z0-9\.-]+)/([0-9a-zA-Z\-\._]+/)*(?<filename>[_\-a-zA-Z0-9\.]+)", RegexOptions.Compiled);

        public string Server { get; private set; }
        public string FileName { get; private set; }
        
        private VideoUrlHelper() { }

        public static VideoUrlHelper Parse(string videoUrl)
        {
            Match match = videoUrlRegex.Match(videoUrl);
            if (match.Success)
            {
                string server = match.Groups["server"].Value;
                string fileName = match.Groups["filename"].Value;

                VideoUrlHelper helper = new VideoUrlHelper
                {
                    Server = server,
                    FileName = fileName
                };

                return helper;
            }

            return null;
        }
    }
}
