using System;
using Model.Microsoft;

namespace Model.Canonical
{
    public class FaceAnalysis
    {
        public double Proportion { get; set; }
        public double Smile { get; set; }
        public string Gender { get; set; }
        public double Age { get; set; }
        public double Roll { get; set; }
        public double Yaw { get; set; }
        public Emotion Emotion { get; set; }
        public Makeup Makeup { get; set; }
        public HairAnalysis Hair { get; set; }

        public FaceAnalysis(Face msFace, Metadata metadata)
        {
            Proportion = Math.Sqrt(Math.Pow(msFace.FaceRectangle.Width, 2) + Math.Pow(msFace.FaceRectangle.Height, 2)) /
                         Math.Sqrt(Math.Pow(metadata.Width, 2) + Math.Pow(metadata.Height, 2));
            Smile = msFace.FaceAttributes.Smile;
            Gender = msFace.FaceAttributes.Gender;
            Age = msFace.FaceAttributes.Age;
            Roll = msFace.FaceAttributes.HeadPose.Roll;
            Yaw = msFace.FaceAttributes.HeadPose.Yaw;
            Emotion = msFace.FaceAttributes.Emotion;
            Makeup = msFace.FaceAttributes.Makeup;
            Hair = new HairAnalysis(msFace.FaceAttributes.Hair);
        }
    }
}