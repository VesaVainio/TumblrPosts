using System.Text.RegularExpressions;

namespace TumblrPics.Model
{
    public class PhotoUrlHelper
    {
        private static Regex tumblrPhotoUrlRegex = new Regex(@"^https://(?<server>\d+).media.tumblr.com/((?<container>[0-9a-f]+)/){0,1}((tumblr_(?<name>[_\-\.0-9a-zA-Z]+)_(?<size>\d+)(sq){0,1})|((?<name>[_\-\.0-9a-zA-Z]+)/s(?<size>\d+)x(?<vsize>\d+)[_a-z]*[0-9_a-z]*/(?<hash>[_\-\.0-9a-zA-Z]+)))\.(?<extension>[0-9a-zA-Z]+)$");
        private static Regex picaiPhotoUrlRegex  = new Regex(@"^https://tumblrpics.blob.core.windows.net/(?<container>[0-9a-z\-]+)/(?<name>[_\-\.0-9a-zA-Z]+)_(?<size>\d+).(?<extension>[0-9a-zA-Z]+)$");

        public int Server { get; private set; }
        public string Container { get; private set; }
        public string Name { get; private set; }
        public int Size { get; private set; }
        public int? VSize { get; private set; }
        public string Hash { get; private set; }
        public string Extension { get; private set; }
        
        private PhotoUrlHelper() { }

        public static PhotoUrlHelper ParseTumblr(string photoUrl)
        {
            Match match = tumblrPhotoUrlRegex.Match(photoUrl);
            if (match.Success)
            {
                string server = match.Groups["server"].Value;
                string container = string.IsNullOrEmpty(match.Groups["container"].Value) ? null : match.Groups["container"].Value;
                string hash = string.IsNullOrEmpty(match.Groups["hash"].Value) ? null : match.Groups["hash"].Value;
                string name = match.Groups["name"].Value;
                string size = match.Groups["size"].Value;
                string vsize = string.IsNullOrEmpty(match.Groups["vsize"].Value) ? null : match.Groups["vsize"].Value;
                string extension = match.Groups["extension"].Value;

                int serverInt = int.Parse(server);
                int sizeInt = int.Parse(size);
                int? vsizeInt = null;

                if (vsize != null)
                {
                    vsizeInt = int.Parse(vsize);
                }

                PhotoUrlHelper helper = new PhotoUrlHelper
                {
                    Server = serverInt,
                    Container = container,
                    Name = name,
                    Size = sizeInt,
                    VSize = vsizeInt,
                    Hash = hash,
                    Extension = extension
                };

                return helper;
            }

            return null;
        }

        public static PhotoUrlHelper ParsePicai(string photoUrl)
        {
            Match match = picaiPhotoUrlRegex.Match(photoUrl);
            if (match.Success)
            {
                string container = match.Groups["container"].Value;
                string name = match.Groups["name"].Value;
                string size = match.Groups["size"].Value;
                string extension = match.Groups["extension"].Value;

                int sizeInt = int.Parse(size);

                PhotoUrlHelper helper = new PhotoUrlHelper
                {
                    Container = container,
                    Name = name,
                    Size = sizeInt,
                    Extension = extension
                };

                return helper;
            }

            return null;
        }
    }
}
