using System.Collections.Generic;

namespace Model.Google
{
    public class WebDetection
    {
        public IList<WebEntity> WebEntities { get; set; }
        public IList<VisuallySimilarImage> VisuallySimilarImages { get; set; }
        public IList<BestGuessLabel> BestGuessLabels { get; set; }
    }
}