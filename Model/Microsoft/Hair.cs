using System.Collections.Generic;

namespace Model.Microsoft
{
    public class Hair
    {
        public double Bald { get; set; }
        public bool Invisible { get; set; }
        public IList<HairColor> HairColor { get; set; }
    }
}