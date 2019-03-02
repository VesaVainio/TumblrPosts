namespace Model.Google
{
    public class LabelAnnotation : ILabel
    {
        public string Mid { get; set; }
        public string Description { get; set; }
        public double Score { get; set; }
        public double Topicality { get; set; }
    }
}