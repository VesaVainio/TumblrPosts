using System.Collections.Generic;
using System.Linq;
using Model.Google;
using Model.Microsoft;

namespace Model.Canonical
{
    public class ImageAnalysis
    {
        public string[] TokenizedText { get; set; }

        public int WordCount { get; set; }

        public Dictionary<string, decimal> Labels { get; set; }

        public int Width { get; set; }
        public int Height { get; set; }

        public List<FaceAnalysis> Faces { get; set; }

        public int FemaleFaces { get; set; }
        public int MaleFaces { get; set; }

        public ImageAnalysis(Response visionResponse, Analysis msAnalysis, List<Face> faces)
        {
            TokenizedText = StringTokenizer.Tokenize(visionResponse.FullTextAnnotation.Text);
            WordCount = TokenizedText.Length;

            Labels = new Dictionary<string, decimal>();
            AddLabels(visionResponse.LabelAnnotations);
            AddLabels(visionResponse.WebDetection.WebEntities);

            Width = msAnalysis.Metadata.Width;
            Height = msAnalysis.Metadata.Height;

            Faces = faces.Take(4).Select(x => new FaceAnalysis(x, msAnalysis.Metadata)).ToList();
            FemaleFaces = faces.Count(x => x.FaceAttributes.Gender.Equals("female"));
            MaleFaces = faces.Count(x => x.FaceAttributes.Gender.Equals("male"));
        }

        public void AddLabels(IEnumerable<ILabel> labels)
        {
            foreach (ILabel labelAnnotation in labels)
            {
                if (Labels.TryGetValue(labelAnnotation.Description, out decimal score))
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
}