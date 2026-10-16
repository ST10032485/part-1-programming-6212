using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Dtos;

// ---------- Requests (data the API expects) ----------

public class RegisterRequest
{
    [Required, MaxLength(50)] public string FirstName { get; set; } = "";
    [Required, MaxLength(50)] public string LastName { get; set; } = "";
    [Required, EmailAddress, MaxLength(100)] public string Email { get; set; } = "";
    [Required, MinLength(8)] public string Password { get; set; } = "";
    /// <summary>Either "Organiser" or "Participant".</summary>
    [Required] public string Role { get; set; } = "";
    [MaxLength(20)] public string? Phone { get; set; }
    public DateOnly? DateOfBirth { get; set; }
}

public class LoginRequest
{
    [Required] public string Email { get; set; } = "";
    [Required] public string Password { get; set; } = "";
}

public class UpdateProfileRequest
{
    [Required, MaxLength(50)] public string FirstName { get; set; } = "";
    [Required, MaxLength(50)] public string LastName { get; set; } = "";
    [MaxLength(20)] public string? Phone { get; set; }
    public DateOnly? DateOfBirth { get; set; }
}

public class CategoryRequest
{
    [Required, MaxLength(50)] public string CategoryName { get; set; } = "";
    /// <summary>Leave empty for age categories such as "Under 20".</summary>
    [Range(0.01, 1000)] public decimal? DistanceKm { get; set; }
    [MaxLength(255)] public string? Description { get; set; }
}

public class EventRequest
{
    [Required, MaxLength(100)] public string Title { get; set; } = "";
    [MaxLength(500)] public string? Description { get; set; }
    [Required, MaxLength(100)] public string Venue { get; set; } = "";
    [Required, MaxLength(50)] public string City { get; set; } = "";
    [Required] public DateOnly? EventDate { get; set; }
    [Required] public DateOnly? RegistrationDeadline { get; set; }
    /// <summary>Run, Walk or Cycle.</summary>
    [Required] public string EventType { get; set; } = "";
    [Range(0.01, 1000)] public decimal DistanceKm { get; set; }
    /// <summary>Only used when updating: Upcoming, Completed or Cancelled.</summary>
    public string? Status { get; set; }
}

public class AddEventCategoryRequest
{
    [Range(1, int.MaxValue)] public int CategoryId { get; set; }
    [Range(0, 100000)] public decimal EntryFee { get; set; }
    [Range(1, 100000)] public int MaxParticipants { get; set; }
}

public class EnrolRequest
{
    [Range(1, int.MaxValue)] public int CategoryId { get; set; }
}

public class UpdateResultRequest
{
    /// <summary>Format hh:mm:ss, for example 00:48:32.</summary>
    public TimeSpan? FinishTime { get; set; }
    [Range(1, 100000)] public int? Position { get; set; }
    /// <summary>Finished, DNF or DNS.</summary>
    [Required] public string Status { get; set; } = "Finished";
}

public class CreateResultRequest : UpdateResultRequest
{
    [Range(1, int.MaxValue)] public int EnrolmentId { get; set; }
}

// ---------- Responses (data the API returns) ----------

public record AuthResponse(string Token, int UserId, string Email, string Role);

public record UserProfileDto(int UserId, string FirstName, string LastName, string Email,
    string? Phone, DateOnly? DateOfBirth, string Role);

public record CategoryDto(int CategoryId, string CategoryName, decimal? DistanceKm, string? Description);

public record EventCategoryDto(int EventCategoryId, int CategoryId, string CategoryName,
    decimal EntryFee, int MaxParticipants);

public record EventDto(int EventId, int OrganiserId, string Title, string? Description, string Venue,
    string City, DateOnly EventDate, DateOnly RegistrationDeadline, string EventType,
    decimal DistanceKm, string Status, List<EventCategoryDto> Categories);

public record EnrolmentDto(int EnrolmentId, int EventId, string EventTitle, int UserId,
    string ParticipantName, int CategoryId, string CategoryName, int BibNumber,
    DateTime EnrolmentDate, string Status);

public record ResultDto(int ResultId, int EnrolmentId, int EventId, string EventTitle,
    string ParticipantName, string CategoryName, int BibNumber, TimeSpan? FinishTime,
    int? Position, string Status);
