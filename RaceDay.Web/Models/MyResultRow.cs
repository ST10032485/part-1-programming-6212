public class MyResultRow
{
    public ResultItem Result { get; set; } = new();
    public DateOnly? EventDate { get; set; }
    public int TotalFinishers { get; set; }
}