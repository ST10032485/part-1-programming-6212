using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly TokenService _tokens;

    public AuthController(AppDbContext db, TokenService tokens)
    {
        _db = db;
        _tokens = tokens;
    }

    /// <summary>Creates a new account as an Organiser or a Participant.</summary>
    /// <remarks>Send your details and a role of "Organiser" or "Participant". The password is saved hashed.</remarks>
    /// <response code="201">Account created. Returns the new profile.</response>
    /// <response code="400">Invalid data or invalid role.</response>
    /// <response code="409">The email is already registered.</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        if (request.Role != AppRoles.Organiser && request.Role != AppRoles.Participant)
            return BadRequest("Role must be 'Organiser' or 'Participant'.");

        var email = request.Email.Trim().ToLower();
        if (await _db.Users.AnyAsync(u => u.Email == email))
            return Conflict("That email is already registered.");

        var role = await _db.Roles.FirstAsync(r => r.RoleName == request.Role);

        var user = new AppUser
        {
            RoleId = role.RoleId,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Phone = request.Phone,
            DateOfBirth = request.DateOfBirth
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var dto = new UserProfileDto(user.UserId, user.FirstName, user.LastName, user.Email,
            user.Phone, user.DateOfBirth, role.RoleName);
        return Created("/api/users/me", dto);
    }

    /// <summary>Logs in and returns a JWT token.</summary>
    /// <remarks>Send your email and password. Use the token as a Bearer token on all protected endpoints.</remarks>
    /// <response code="200">Login successful. Returns the token and the role.</response>
    /// <response code="401">Wrong email or password.</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        var email = request.Email.Trim().ToLower();
        var user = await _db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Email == email);

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Unauthorized("Wrong email or password.");

        var token = _tokens.CreateToken(user, user.Role!.RoleName);
        return new AuthResponse(token, user.UserId, user.Email, user.Role.RoleName);
    }
}
