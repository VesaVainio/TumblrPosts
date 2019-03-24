namespace Model.Google
{
    public class LabelAnnotation : ILabel
    {
        public string Mid { get; set; }
        public string Description { get; set; }
        public decimal Score { get; set; }
    }
}