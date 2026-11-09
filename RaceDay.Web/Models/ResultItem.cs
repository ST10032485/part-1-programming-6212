public class ResultItem
{
    public int ResultId { get; set; }
    public int EnrolmentId { get; set; }
    public int EventId { get; set; }
    public string EventTitle { get; set; } = "";
    public string ParticipantName { get; set; } = "";
    public string CategoryName { get; set; } = "";
    public int BibNumber { get; set; }
    public TimeSpan? FinishTime { get; set; }
    public int? Position { get; set; }
    public string Status { get; set; } = "";
}