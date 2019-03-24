namespace Model.Google
{
    public class WebEntity : ILabel
    {
        public string EntityId { get; set; }
        public decimal Score { get; set; }
        public string Description { get; set; }
    }
}