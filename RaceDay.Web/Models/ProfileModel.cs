using System.ComponentModel.DataAnnotations;

public class ProfileModel
{
    public int UserId { get; set; }
    [Required, MaxLength(50)] public string FirstName { get; set; } = "";
    [Required, MaxLength(50)] public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
    [MaxLength(20)] public string? Phone { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string Role { get; set; } = "";
}