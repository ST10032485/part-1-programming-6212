using System.ComponentModel.DataAnnotations;

public class RegisterViewModel
{
    [Required, MaxLength(50)] public string FirstName { get; set; } = "";
    [Required, MaxLength(50)] public string LastName { get; set; } = "";
    [Required, EmailAddress] public string Email { get; set; } = "";
    [Required, MinLength(8)] public string Password { get; set; } = "";
    [Required] public string Role { get; set; } = "Participant";
    [MaxLength(20)] public string? Phone { get; set; }
    public DateOnly? DateOfBirth { get; set; }
}
