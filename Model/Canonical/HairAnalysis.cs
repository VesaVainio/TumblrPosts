using System;
using Model.Microsoft;

namespace Model.Canonical
{
    public class HairAnalysis
    {
        public decimal HairBlond { get; set; }
        public decimal HairBrown { get; set; }
        public decimal HairRed { get; set; }
        public decimal HairGray { get; set; }
        public decimal HairOther { get; set; }
        public decimal HairBlack { get; set; }
        public decimal Bald { get; set; }

        public HairAnalysis() { }

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