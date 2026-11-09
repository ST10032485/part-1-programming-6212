using System.ComponentModel.DataAnnotations;

public class AddCategoryModel
{
    [Range(1, int.MaxValue)] public int CategoryId { get; set; }
    [Range(0, 100000)] public decimal EntryFee { get; set; }
    [Range(1, int.MaxValue)] public int MaxParticipants { get; set; }
}