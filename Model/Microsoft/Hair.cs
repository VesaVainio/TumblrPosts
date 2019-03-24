using System.Collections.Generic;

namespace Model.Microsoft
{
    public class Hair
    {
        public decimal Bald { get; set; }
        public bool Invisible { get; set; }
        public IList<HairColor> HairColor { get; set; }
    }
}