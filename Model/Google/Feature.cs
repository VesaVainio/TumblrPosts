using System.Diagnostics.CodeAnalysis;

namespace Model.Google
{
    public class Feature
    {
        public string Type { get; set; }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public int MaxResults { get; set; }
    }
}