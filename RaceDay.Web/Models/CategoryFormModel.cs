using System.ComponentModel.DataAnnotations;

public class CategoryFormModel
{
    [Required, MaxLength(50)] public string CategoryName { get; set; } = "";
    [Range(0.01, 1000)] public decimal? DistanceKm { get; set; }
    [MaxLength(255)] public string? Description { get; set; }
}