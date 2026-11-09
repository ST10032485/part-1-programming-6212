public class DashboardEventRow
{
    public EventItem Event { get; set; } = new();
    public int EnrolmentCount { get; set; }
}

public class DashboardViewModel
{
    public List<DashboardEventRow> Rows { get; set; } = new();
    public int TotalEvents { get; set; }
    public int UpcomingEvents { get; set; }
    public int TotalEnrolments { get; set; }
    public EventItem? NextEvent { get; set; }
}