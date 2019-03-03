using System.Collections.Generic;

namespace Model.Microsoft
{
    public class Color
    {
        public string DominantColorForeground { get; set; }
        public string DominantColorBackground { get; set; }
        public IList<string> DominantColors { get; set; }
        public string AccentColor { get; set; }
        public bool IsBwImg { get; set; }
        public bool IsBWImg { get; set; }
    }
}