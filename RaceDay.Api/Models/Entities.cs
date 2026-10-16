using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Models;

public static class AppRoles
{
    public const string Organiser = "Organiser";
    public const string Participant = "Participant";
}

public class Role
{
    public int RoleId { get; set; }
    [MaxLength(30)] public string RoleName { get; set; } = "";
}

// Maps to the Users table (named AppUser so it does not clash with ControllerBase.User)
public class AppUser
{
    public int UserId { get; set; }
    public int RoleId { get; set; }
    public Role? Role { get; set; }
    [MaxLength(50)] public string FirstName { get; set; } = "";
    [MaxLength(50)] public string LastName { get; set; } = "";
    [MaxLength(100)] public string Email { get; set; } = "";
    [MaxLength(255)] public string PasswordHash { get; set; } = "";
    [MaxLength(20)] public string? Phone { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

public class Category
{
    public int CategoryId { get; set; }
    [MaxLength(50)] public string CategoryName { get; set; } = "";
    public decimal? DistanceKm { get; set; }   // empty for age categories such as "Under 20"
    [MaxLength(255)] public string? Description { get; set; }
}

public class Event
{
    public int EventId { get; set; }
    public int OrganiserId { get; set; }
    public AppUser? Organiser { get; set; }
    [MaxLength(100)] public string Title { get; set; } = "";
    [MaxLength(500)] public string? Description { get; set; }
    [MaxLength(100)] public string Venue { get; set; } = "";
    [MaxLength(50)] public string City { get; set; } = "";
    public DateOnly EventDate { get; set; }
    public DateOnly RegistrationDeadline { get; set; }
    [MaxLength(10)] public string EventType { get; set; } = "Run";   // Run, Walk or Cycle
    public decimal DistanceKm { get; set; }
    [MaxLength(20)] public string Status { get; set; } = "Upcoming";  // Upcoming, Completed or Cancelled
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public List<EventCategory> EventCategories { get; set; } = new();
}

public class EventCategory
{
    public int EventCategoryId { get; set; }
    public int EventId { get; set; }
    public Event? Event { get; set; }
    public int CategoryId { get; set; }
    public Category? Category { get; set; }
    public decimal EntryFee { get; set; }
    public int MaxParticipants { get; set; } = 100;
}

public class Enrolment
{
    public int EnrolmentId { get; set; }
    public int UserId { get; set; }
    public AppUser? User { get; set; }
    public int EventCategoryId { get; set; }
    public EventCategory? EventCategory { get; set; }
    public int BibNumber { get; set; }
    public DateTime EnrolmentDate { get; set; } = DateTime.Now;
    [MaxLength(20)] public string Status { get; set; } = "Confirmed";  // Confirmed or Cancelled
    public Result? Result { get; set; }
}

public class Result
{
    public int ResultId { get; set; }
    public int EnrolmentId { get; set; }
    public Enrolment? Enrolment { get; set; }
    public TimeSpan? FinishTime { get; set; }
    public int? Position { get; set; }
    [MaxLength(20)] public string Status { get; set; } = "Finished";  // Finished, DNF or DNS
    public DateTime RecordedAt { get; set; } = DateTime.Now;
}
