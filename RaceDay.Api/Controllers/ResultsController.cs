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
public class ResultsController : ControllerBase
{
    private readonly AppDbContext _db;

    public ResultsController(AppDbContext db)
    {
        _db = db;
    }

    private static readonly Expression<Func<Result, ResultDto>> ToDto = r => new ResultDto(
        r.ResultId, r.EnrolmentId, r.Enrolment!.EventCategory!.EventId,
        r.Enrolment.EventCategory.Event!.Title,
        r.Enrolment.User!.FirstName + " " + r.Enrolment.User.LastName,
        r.Enrolment.EventCategory.Category!.CategoryName, r.Enrolment.BibNumber,
        r.FinishTime, r.Position, r.Status);

    private static readonly string[] ResultStatuses = { "Finished", "DNF", "DNS" };

    private static string? Validate(UpdateResultRequest r)
    {
        if (!ResultStatuses.Contains(r.Status))
            return "Status must be Finished, DNF or DNS.";
        if (r.Status == "Finished" && r.FinishTime == null)
            return "FinishTime is required when Status is Finished.";
        return null;
    }

    private Task<ResultDto> GetDto(int id) =>
        _db.Results.Where(r => r.ResultId == id).Select(ToDto).FirstAsync();

    /// <summary>Records a finish time and position for an enrolment. Only the event's Organiser.</summary>
    /// <remarks>Send enrolmentId, finishTime (hh:mm:ss), position and status (Finished, DNF or DNS).</remarks>
    /// <response code="201">Result recorded.</response>
    /// <response code="400">Invalid data, or the enrolment is cancelled.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or not your event.</response>
    /// <response code="404">Enrolment not found.</response>
    /// <response code="409">A result already exists for this enrolment.</response>
    [HttpPost("results")]
    [Authorize(Roles = AppRoles.Organiser)]
    [ProducesResponseType(typeof(ResultDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ResultDto>> Create(CreateResultRequest request)
    {
        var enrolment = await _db.Enrolments
            .Include(e => e.EventCategory).ThenInclude(ec => ec!.Event)
            .FirstOrDefaultAsync(e => e.EnrolmentId == request.EnrolmentId);
        if (enrolment == null) return NotFound("Enrolment not found.");
        if (enrolment.EventCategory!.Event!.OrganiserId != User.GetUserId()) return Forbid();

        if (enrolment.Status != "Confirmed")
            return BadRequest("This enrolment was cancelled.");
        if (await _db.Results.AnyAsync(r => r.EnrolmentId == request.EnrolmentId))
            return Conflict("A result already exists for this enrolment.");

        var error = Validate(request);
        if (error != null) return BadRequest(error);

        var result = new Result
        {
            EnrolmentId = request.EnrolmentId,
            FinishTime = request.FinishTime,
            Position = request.Position,
            Status = request.Status
        };
        _db.Results.Add(result);
        await _db.SaveChangesAsync();

        return Created($"/api/events/{enrolment.EventCategory.EventId}/results", await GetDto(result.ResultId));
    }

    /// <summary>Updates a result. Only the event's Organiser.</summary>
    /// <response code="200">The updated result.</response>
    /// <response code="400">Invalid data.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or not your event.</response>
    /// <response code="404">Result not found.</response>
    [HttpPut("results/{id:int}")]
    [Authorize(Roles = AppRoles.Organiser)]
    [ProducesResponseType(typeof(ResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResultDto>> Update(int id, UpdateResultRequest request)
    {
        var result = await _db.Results
            .Include(r => r.Enrolment).ThenInclude(e => e!.EventCategory).ThenInclude(ec => ec!.Event)
            .FirstOrDefaultAsync(r => r.ResultId == id);
        if (result == null) return NotFound();
        if (result.Enrolment!.EventCategory!.Event!.OrganiserId != User.GetUserId()) return Forbid();

        var error = Validate(request);
        if (error != null) return BadRequest(error);

        result.FinishTime = request.FinishTime;
        result.Position = request.Position;
        result.Status = request.Status;
        await _db.SaveChangesAsync();

        return await GetDto(id);
    }

    /// <summary>Deletes a result. Only the event's Organiser.</summary>
    /// <response code="204">Result deleted.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or not your event.</response>
    /// <response code="404">Result not found.</response>
    [HttpDelete("results/{id:int}")]
    [Authorize(Roles = AppRoles.Organiser)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _db.Results
            .Include(r => r.Enrolment).ThenInclude(e => e!.EventCategory).ThenInclude(ec => ec!.Event)
            .FirstOrDefaultAsync(r => r.ResultId == id);
        if (result == null) return NotFound();
        if (result.Enrolment!.EventCategory!.Event!.OrganiserId != User.GetUserId()) return Forbid();

        _db.Results.Remove(result);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Returns the results of an event, ordered by category and position.</summary>
    /// <param name="eventId">The event to look at.</param>
    /// <response code="200">List of results.</response>
    /// <response code="404">Event not found.</response>
    [HttpGet("events/{eventId:int}/results")]
    [ProducesResponseType(typeof(List<ResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<ResultDto>>> GetForEvent(int eventId)
    {
        if (!await _db.Events.AnyAsync(e => e.EventId == eventId)) return NotFound();

        return await _db.Results
            .Where(r => r.Enrolment!.EventCategory!.EventId == eventId)
            .OrderBy(r => r.Enrolment!.EventCategory!.Category!.CategoryName)
            .ThenBy(r => r.Position ?? int.MaxValue)
            .Select(ToDto).ToListAsync();
    }

    /// <summary>Returns the results of the logged in Participant.</summary>
    /// <response code="200">List of the participant's results.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not a Participant.</response>
    [HttpGet("results/mine")]
    [Authorize(Roles = AppRoles.Participant)]
    [ProducesResponseType(typeof(List<ResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<ResultDto>>> GetMine()
    {
        var userId = User.GetUserId();
        return await _db.Results.Where(r => r.Enrolment!.UserId == userId)
            .OrderBy(r => r.ResultId).Select(ToDto).ToListAsync();
    }
}
