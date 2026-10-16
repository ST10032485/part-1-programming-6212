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
[Route("api/events")]
public class EventsController : ControllerBase
{
    private readonly AppDbContext _db;

    public EventsController(AppDbContext db)
    {
        _db = db;
    }

    // Turns an Event into the EventDto sent back to the client
    private static readonly Expression<Func<Event, EventDto>> ToDto = e => new EventDto(
        e.EventId, e.OrganiserId, e.Title, e.Description, e.Venue, e.City,
        e.EventDate, e.RegistrationDeadline, e.EventType, e.DistanceKm, e.Status,
        e.EventCategories
            .Select(ec => new EventCategoryDto(ec.EventCategoryId, ec.CategoryId,
                ec.Category!.CategoryName, ec.EntryFee, ec.MaxParticipants))
            .ToList());

    private static readonly string[] EventTypes = { "Run", "Walk", "Cycle" };
    private static readonly string[] EventStatuses = { "Upcoming", "Completed", "Cancelled" };

    private static string? Validate(EventRequest r, bool isUpdate)
    {
        if (!EventTypes.Contains(r.EventType))
            return "EventType must be Run, Walk or Cycle.";
        if (r.RegistrationDeadline > r.EventDate)
            return "RegistrationDeadline cannot be after EventDate.";
        if (isUpdate && r.Status != null && !EventStatuses.Contains(r.Status))
            return "Status must be Upcoming, Completed or Cancelled.";
        return null;
    }

    private Task<EventDto> GetDto(int id) =>
        _db.Events.Where(e => e.EventId == id).Select(ToDto).FirstAsync();

    /// <summary>Lists events. Optional filters: city, status and date.</summary>
    /// <param name="city">Only events in this city.</param>
    /// <param name="status">Upcoming, Completed or Cancelled.</param>
    /// <param name="date">Only events on this date (yyyy-MM-dd).</param>
    /// <response code="200">List of events with their categories.</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<EventDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<EventDto>>> GetAll(string? city, string? status, DateOnly? date)
    {
        var query = _db.Events.AsQueryable();
        if (!string.IsNullOrWhiteSpace(city)) query = query.Where(e => e.City == city);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(e => e.Status == status);
        if (date != null) query = query.Where(e => e.EventDate == date);

        return await query.OrderBy(e => e.EventDate).Select(ToDto).ToListAsync();
    }

    /// <summary>Lists the events created by the logged in Organiser.</summary>
    /// <response code="200">List of the organiser's events.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser.</response>
    [HttpGet("mine")]
    [Authorize(Roles = AppRoles.Organiser)]
    [ProducesResponseType(typeof(List<EventDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<EventDto>>> GetMine()
    {
        var userId = User.GetUserId();
        return await _db.Events.Where(e => e.OrganiserId == userId)
            .OrderBy(e => e.EventDate).Select(ToDto).ToListAsync();
    }

    /// <summary>Returns one event with its categories.</summary>
    /// <response code="200">The event.</response>
    /// <response code="404">Event not found.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventDto>> GetById(int id)
    {
        var dto = await _db.Events.Where(e => e.EventId == id).Select(ToDto).FirstOrDefaultAsync();
        if (dto == null) return NotFound();
        return dto;
    }

    /// <summary>Creates a new event. Organisers only.</summary>
    /// <remarks>Send title, venue, city, eventDate, registrationDeadline, eventType (Run, Walk or Cycle) and distanceKm. Description is optional.</remarks>
    /// <response code="201">Event created.</response>
    /// <response code="400">Invalid data.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser.</response>
    [HttpPost]
    [Authorize(Roles = AppRoles.Organiser)]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<EventDto>> Create(EventRequest request)
    {
        var error = Validate(request, false);
        if (error != null) return BadRequest(error);

        var ev = new Event
        {
            OrganiserId = User.GetUserId(),
            Title = request.Title.Trim(),
            Description = request.Description,
            Venue = request.Venue.Trim(),
            City = request.City.Trim(),
            EventDate = request.EventDate!.Value,
            RegistrationDeadline = request.RegistrationDeadline!.Value,
            EventType = request.EventType,
            DistanceKm = request.DistanceKm,
            Status = "Upcoming"
        };
        _db.Events.Add(ev);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = ev.EventId }, await GetDto(ev.EventId));
    }

    /// <summary>Updates an event, including its status. Only the Organiser who created it.</summary>
    /// <response code="200">The updated event.</response>
    /// <response code="400">Invalid data.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or not your event.</response>
    /// <response code="404">Event not found.</response>
    [HttpPut("{id:int}")]
    [Authorize(Roles = AppRoles.Organiser)]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventDto>> Update(int id, EventRequest request)
    {
        var ev = await _db.Events.FindAsync(id);
        if (ev == null) return NotFound();
        if (ev.OrganiserId != User.GetUserId()) return Forbid();

        var error = Validate(request, true);
        if (error != null) return BadRequest(error);

        ev.Title = request.Title.Trim();
        ev.Description = request.Description;
        ev.Venue = request.Venue.Trim();
        ev.City = request.City.Trim();
        ev.EventDate = request.EventDate!.Value;
        ev.RegistrationDeadline = request.RegistrationDeadline!.Value;
        ev.EventType = request.EventType;
        ev.DistanceKm = request.DistanceKm;
        if (request.Status != null) ev.Status = request.Status;
        await _db.SaveChangesAsync();

        return await GetDto(id);
    }

    /// <summary>Deletes an event that has no enrolments. Only the Organiser who created it.</summary>
    /// <response code="204">Event deleted.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or not your event.</response>
    /// <response code="404">Event not found.</response>
    /// <response code="409">The event already has enrolments.</response>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = AppRoles.Organiser)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id)
    {
        var ev = await _db.Events.Include(e => e.EventCategories).FirstOrDefaultAsync(e => e.EventId == id);
        if (ev == null) return NotFound();
        if (ev.OrganiserId != User.GetUserId()) return Forbid();

        if (await _db.Enrolments.AnyAsync(en => en.EventCategory!.EventId == id))
            return Conflict("This event already has enrolments.");

        _db.EventCategories.RemoveRange(ev.EventCategories);
        _db.Events.Remove(ev);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Adds a category to an event with an entry fee and a participant limit. Only the event's Organiser.</summary>
    /// <response code="201">Category added to the event.</response>
    /// <response code="400">Invalid data.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or not your event.</response>
    /// <response code="404">Event or category not found.</response>
    /// <response code="409">The category is already added to this event.</response>
    [HttpPost("{id:int}/categories")]
    [Authorize(Roles = AppRoles.Organiser)]
    [ProducesResponseType(typeof(EventCategoryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EventCategoryDto>> AddCategory(int id, AddEventCategoryRequest request)
    {
        var ev = await _db.Events.FindAsync(id);
        if (ev == null) return NotFound("Event not found.");
        if (ev.OrganiserId != User.GetUserId()) return Forbid();

        var category = await _db.Categories.FindAsync(request.CategoryId);
        if (category == null) return NotFound("Category not found.");

        if (await _db.EventCategories.AnyAsync(ec => ec.EventId == id && ec.CategoryId == request.CategoryId))
            return Conflict("This category is already added to the event.");

        var eventCategory = new EventCategory
        {
            EventId = id,
            CategoryId = request.CategoryId,
            EntryFee = request.EntryFee,
            MaxParticipants = request.MaxParticipants
        };
        _db.EventCategories.Add(eventCategory);
        await _db.SaveChangesAsync();

        var dto = new EventCategoryDto(eventCategory.EventCategoryId, category.CategoryId,
            category.CategoryName, eventCategory.EntryFee, eventCategory.MaxParticipants);
        return Created($"/api/events/{id}", dto);
    }

    /// <summary>Removes a category from an event. Only the event's Organiser.</summary>
    /// <response code="204">Category removed from the event.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or not your event.</response>
    /// <response code="404">Event or category link not found.</response>
    /// <response code="409">The category already has enrolments.</response>
    [HttpDelete("{id:int}/categories/{categoryId:int}")]
    [Authorize(Roles = AppRoles.Organiser)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RemoveCategory(int id, int categoryId)
    {
        var ev = await _db.Events.FindAsync(id);
        if (ev == null) return NotFound("Event not found.");
        if (ev.OrganiserId != User.GetUserId()) return Forbid();

        var eventCategory = await _db.EventCategories
            .FirstOrDefaultAsync(ec => ec.EventId == id && ec.CategoryId == categoryId);
        if (eventCategory == null) return NotFound("That category is not part of this event.");

        if (await _db.Enrolments.AnyAsync(en => en.EventCategoryId == eventCategory.EventCategoryId))
            return Conflict("This category already has enrolments.");

        _db.EventCategories.Remove(eventCategory);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
