using System.Linq.Expressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api")]
[Authorize]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public class EnrolmentsController : ControllerBase
{
    private readonly AppDbContext _db;

    public EnrolmentsController(AppDbContext db)
    {
        _db = db;
    }

    private static readonly Expression<Func<Enrolment, EnrolmentDto>> ToDto = e => new EnrolmentDto(
        e.EnrolmentId, e.EventCategory!.EventId, e.EventCategory.Event!.Title, e.UserId,
        e.User!.FirstName + " " + e.User.LastName, e.EventCategory.CategoryId,
        e.EventCategory.Category!.CategoryName, e.BibNumber, e.EnrolmentDate, e.Status);

    /// <summary>Enrols the logged in Participant in a category of an event.</summary>
    /// <remarks>Send the categoryId. A bib number is assigned automatically. Enrolment closes after the registration deadline.</remarks>
    /// <param name="eventId">The event to enter.</param>
    /// <response code="201">Enrolled. Returns the enrolment with the bib number.</response>
    /// <response code="400">Registration is closed for this event.</response>
    /// <response code="403">Not a Participant.</response>
    /// <response code="404">The event or the category is not found.</response>
    /// <response code="409">Already enrolled, or the category is full.</response>
    [HttpPost("events/{eventId:int}/enrol")]
    [Authorize(Roles = AppRoles.Participant)]
    [ProducesResponseType(typeof(EnrolmentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EnrolmentDto>> Enrol(int eventId, EnrolRequest request)
    {
        var eventCategory = await _db.EventCategories.Include(ec => ec.Event)
            .FirstOrDefaultAsync(ec => ec.EventId == eventId && ec.CategoryId == request.CategoryId);
        if (eventCategory == null)
            return NotFound("Event or category not found.");

        var today = DateOnly.FromDateTime(DateTime.Today);
        if (eventCategory.Event!.Status != "Upcoming" || today > eventCategory.Event.RegistrationDeadline)
            return BadRequest("Registration is closed for this event.");

        var userId = User.GetUserId();
        var existing = await _db.Enrolments
            .FirstOrDefaultAsync(e => e.UserId == userId && e.EventCategoryId == eventCategory.EventCategoryId);
        if (existing != null && existing.Status == "Confirmed")
            return Conflict("You are already enrolled in this category.");

        var confirmedCount = await _db.Enrolments
            .CountAsync(e => e.EventCategoryId == eventCategory.EventCategoryId && e.Status == "Confirmed");
        if (confirmedCount >= eventCategory.MaxParticipants)
            return Conflict("This category is full.");

        Enrolment enrolment;
        if (existing != null)
        {
            // The participant cancelled earlier and is joining again
            existing.Status = "Confirmed";
            enrolment = existing;
        }
        else
        {
            var lastBib = await _db.Enrolments
                .Where(e => e.EventCategoryId == eventCategory.EventCategoryId)
                .MaxAsync(e => (int?)e.BibNumber) ?? 0;

            enrolment = new Enrolment
            {
                UserId = userId,
                EventCategoryId = eventCategory.EventCategoryId,
                BibNumber = lastBib + 1
            };
            _db.Enrolments.Add(enrolment);
        }
        await _db.SaveChangesAsync();

        var dto = await _db.Enrolments.Where(e => e.EnrolmentId == enrolment.EnrolmentId)
            .Select(ToDto).FirstAsync();
        return Created("/api/enrolments/mine", dto);
    }

    /// <summary>Lists the enrolments of the logged in Participant.</summary>
    /// <response code="200">List of enrolments.</response>
    /// <response code="403">Not a Participant.</response>
    [HttpGet("enrolments/mine")]
    [Authorize(Roles = AppRoles.Participant)]
    [ProducesResponseType(typeof(List<EnrolmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<EnrolmentDto>>> GetMine()
    {
        var userId = User.GetUserId();
        return await _db.Enrolments.Where(e => e.UserId == userId)
            .OrderBy(e => e.EnrolmentId).Select(ToDto).ToListAsync();
    }

    /// <summary>Cancels an enrolment. Only the Participant who owns it.</summary>
    /// <response code="204">Enrolment cancelled.</response>
    /// <response code="403">Not a Participant, or not your enrolment.</response>
    /// <response code="404">Enrolment not found.</response>
    /// <response code="409">A result is already recorded for this enrolment.</response>
    [HttpDelete("enrolments/{id:int}")]
    [Authorize(Roles = AppRoles.Participant)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancel(int id)
    {
        var enrolment = await _db.Enrolments.FindAsync(id);
        if (enrolment == null) return NotFound();
        if (enrolment.UserId != User.GetUserId()) return Forbid();

        if (await _db.Results.AnyAsync(r => r.EnrolmentId == id))
            return Conflict("A result is already recorded for this enrolment.");

        enrolment.Status = "Cancelled";
        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Lists everyone enrolled in an event. Only the event's Organiser.</summary>
    /// <param name="eventId">The event to look at.</param>
    /// <response code="200">List of enrolments.</response>
    /// <response code="403">Not an Organiser, or not your event.</response>
    /// <response code="404">Event not found.</response>
    [HttpGet("events/{eventId:int}/enrolments")]
    [Authorize(Roles = AppRoles.Organiser)]
    [ProducesResponseType(typeof(List<EnrolmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<EnrolmentDto>>> GetForEvent(int eventId)
    {
        var ev = await _db.Events.FindAsync(eventId);
        if (ev == null) return NotFound();
        if (ev.OrganiserId != User.GetUserId()) return Forbid();

        return await _db.Enrolments.Where(e => e.EventCategory!.EventId == eventId)
            .OrderBy(e => e.EventCategoryId).ThenBy(e => e.BibNumber)
            .Select(ToDto).ToListAsync();
    }
}
