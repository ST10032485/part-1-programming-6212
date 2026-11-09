using System.ComponentModel.DataAnnotations;

public class EventFormModel
{
    [Required, MaxLength(100)] public string Title { get; set; } = "";
    [MaxLength(500)] public string? Description { get; set; }
    [Required, MaxLength(100)] public string Venue { get; set; } = "";
    [Required, MaxLength(50)] public string City { get; set; } = "";
    [Required] public DateOnly? EventDate { get; set; }
    [Required] public DateOnly? RegistrationDeadline { get; set; }
    [Required] public string EventType { get; set; } = "Run";
    [Required, Range(0.01, 1000)] public decimal? DistanceKm { get; set; }
}