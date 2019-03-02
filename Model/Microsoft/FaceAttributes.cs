using System.Collections.Generic;

namespace Model.Microsoft
{
    public class FaceAttributes
    {
        public double Smile { get; set; }
        public HeadPose HeadPose { get; set; }
        public string Gender { get; set; }
        public double Age { get; set; }
        public FacialHair FacialHair { get; set; }
        public string Glasses { get; set; }
        public Emotion Emotion { get; set; }
        public Blur Blur { get; set; }
        public Exposure Exposure { get; set; }
        public Noise Noise { get; set; }
        public Makeup Makeup { get; set; }
        public IList<object> Accessories { get; set; }
        public Occlusion Occlusion { get; set; }
        public Hair Hair { get; set; }
    }
}