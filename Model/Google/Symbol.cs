namespace Model.Google
{
    public class Symbol
    {
        public Property Property { get; set; }
        public BoundingBox BoundingBox { get; set; }
        public string Text { get; set; }
        public decimal Confidence { get; set; }
    }
}