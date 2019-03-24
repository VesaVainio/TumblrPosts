namespace Model.Microsoft
{
    public class Adult
    {
        public bool IsAdultContent { get; set; }
        public bool IsRacyContent { get; set; }
        public decimal AdultScore { get; set; }
        public decimal RacyScore { get; set; }
    }
}