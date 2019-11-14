using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.Tumblr
{
    public class Content
    {
        public string Type { get; set; }
        public string SubType { get; set; }
        public string Text { get; set; }
        public Formatting[] Formatting { get; set; }
        public Media[] Media { get; set; }
        public Poster[] Poster { get; set; }
        public string Url { get; set; }
    }
}
