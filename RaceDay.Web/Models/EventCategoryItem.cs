public class EventCategoryItem
{
    public int EventCategoryId { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = "";
    public decimal EntryFee { get; set; }
    public int MaxParticipants { get; set; }
}