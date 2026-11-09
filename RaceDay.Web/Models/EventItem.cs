public class EventItem
{
    public int EventId { get; set; }
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public string Venue { get; set; } = "";
    public string City { get; set; } = "";
    public DateOnly EventDate { get; set; }
    public DateOnly RegistrationDeadline { get; set; }
    public string EventType { get; set; } = "";
    public decimal DistanceKm { get; set; }
    public string Status { get; set; } = "";

    public List<EventCategoryItem> Categories { get; set; } = new();
}