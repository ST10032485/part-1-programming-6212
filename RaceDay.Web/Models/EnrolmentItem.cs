public class EnrolmentItem
{
    public int EnrolmentId { get; set; }
    public int EventId { get; set; }
    public string EventTitle { get; set; } = "";
    public int UserId { get; set; }
    public string ParticipantName { get; set; } = "";
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = "";
    public int BibNumber { get; set; }
    public DateTime EnrolmentDate { get; set; }
    public string Status { get; set; } = "";
}