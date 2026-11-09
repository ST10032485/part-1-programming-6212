public class ResultRow
{
    public EnrolmentItem Enrolment { get; set; } = new();
    public ResultItem? Result { get; set; }
}

public class EventResultsViewModel
{
    public int EventId { get; set; }
    public string EventTitle { get; set; } = "";
    public List<ResultRow> Rows { get; set; } = new();
}