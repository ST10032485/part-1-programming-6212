using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _db;

    public UsersController(AppDbContext db)
    {
        _db = db;
    }

    private static UserProfileDto ToDto(AppUser u) =>
        new(u.UserId, u.FirstName, u.LastName, u.Email, u.Phone, u.DateOfBirth, u.Role!.RoleName);

    /// <summary>Returns the profile of the logged in user.</summary>
    /// <response code="200">The profile details.</response>
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<UserProfileDto>> GetMe()
    {
        var userId = User.GetUserId();
        var user = await _db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserId == userId);
        if (user == null) return Unauthorized();
        return ToDto(user);
    }

    /// <summary>Updates the profile of the logged in user.</summary>
    /// <remarks>Send firstName, lastName and optionally phone and dateOfBirth.</remarks>
    /// <response code="200">The updated profile.</response>
    /// <response code="400">Invalid data.</response>
    [HttpPut("me")]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UserProfileDto>> UpdateMe(UpdateProfileRequest request)
    {
        var userId = User.GetUserId();
        var user = await _db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserId == userId);
        if (user == null) return Unauthorized();

        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.Phone = request.Phone;
        user.DateOfBirth = request.DateOfBirth;
        await _db.SaveChangesAsync();

        return ToDto(user);
    }

    /// <summary>Deletes the account of the logged in user.</summary>
    /// <response code="204">Account deleted.</response>
    /// <response code="409">The user still has events or enrolments.</response>
    [HttpDelete("me")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteMe()
    {
        var userId = User.GetUserId();
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return Unauthorized();

        var hasEvents = await _db.Events.AnyAsync(e => e.OrganiserId == userId);
        var hasEnrolments = await _db.Enrolments.AnyAsync(e => e.UserId == userId);
        if (hasEvents || hasEnrolments)
            return Conflict("Remove your events and enrolments first.");

        _db.Users.Remove(user);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
