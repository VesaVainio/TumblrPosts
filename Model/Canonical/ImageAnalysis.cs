using System.Collections.Generic;
using Model.Google;
using Model.Microsoft;

namespace Model.Canonical
{
    public class ImageAnalysis
    {
        public ImageAnalysis(Response visionResponse, List<Face> faces)
        {
            FullText = visionResponse.FullTextAnnotation.Text;

            Labels = new Dictionary<string, double>();
            AddLabels(visionResponse.LabelAnnotations);
            AddLabels(visionResponse.WebDetection.WebEntities);

            Faces = faces;
        }


        public string FullText { get; set; }
        public Dictionary<string, double> Labels { get; set; }
        public List<Face> Faces { get; set; }


        public void AddLabels(IEnumerable<ILabel> labels)
        {
            foreach (ILabel labelAnnotation in labels)
                if (Labels.TryGetValue(labelAnnotation.Description, out double score))
                {
                    if (score < labelAnnotation.Score) Labels[labelAnnotation.Description] = score; // replace smaller with bigger
                }
                else
                {
                    Labels.Add(labelAnnotation.Description, labelAnnotation.Score);
                }
        }
    }
}