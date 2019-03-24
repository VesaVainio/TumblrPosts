using System;
using Model.Microsoft;

namespace Model.Canonical
{
    public class HairAnalysis
    {
        public double HairBlond { get; set; }
        public double HairBrown { get; set; }
        public double HairRed { get; set; }
        public double HairGray { get; set; }
        public double HairOther { get; set; }
        public double HairBlack { get; set; }
        public double Bald { get; set; }

        public HairAnalysis(Hair hair)
        {
            Bald = hair.Bald;
            foreach (HairColor hairColor in hair.HairColor)
                switch (hairColor.Color)
                {
                    case "blond":
                        HairBlond = hairColor.Confidence;
                        break;
                    case "brown":
                        HairBrown = hairColor.Confidence;
                        break;
                    case "red":
                        HairRed = hairColor.Confidence;
                        break;
                    case "gray":
                        HairGray = hairColor.Confidence;
                        break;
                    case "other":
                        HairOther = hairColor.Confidence;
                        break;
                    case "black":
                        HairBlack = hairColor.Confidence;
                        break;
                    default:
                        throw new ArgumentException("Unexpected hair color");
                }
        }
    }
}