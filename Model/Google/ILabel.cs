namespace Model.Google
{
    public interface ILabel
    {
        string Description { get; set; }
        decimal Score { get; set; }
    }
}