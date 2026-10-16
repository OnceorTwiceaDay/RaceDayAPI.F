using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Filters;
using RaceDay.Api.Models;
using RaceDay.Contracts;

namespace RaceDay.Api.Controllers;

[ApiController]
public class ResultsController : RaceDayControllerBase
{
    private readonly RaceDayDbContext _db;

    public ResultsController(RaceDayDbContext db)
    {
        _db = db;
    }

    /// <summary>POST /api/enrolments/{enrolmentId}/result - only the Organiser who owns the event.</summary>
    [HttpPost("api/enrolments/{enrolmentId:int}/result")]
    [RequireRole(Role.Organiser)]
    public async Task<ActionResult<ResultDto>> Create(int enrolmentId, CreateResultRequest request)
    {
        var enrolment = await _db.Enrolments
            .Include(en => en.Event)
            .Include(en => en.Result)
            .FirstOrDefaultAsync(en => en.EnrolmentId == enrolmentId);

        if (enrolment is null) return NotFound(new { message = "Enrolment not found." });
        if (enrolment.Event.OrganiserId != CurrentUserId)
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "You do not own this event." });

        if (enrolment.Result is not null)
        {
            return Conflict(new { message = "A result has already been captured for this enrolment." });
        }

        var validStatuses = new[] { "Finished", "DNF", "DNS" };
        if (!validStatuses.Contains(request.Status))
        {
            return BadRequest(new { message = "Status must be 'Finished', 'DNF' or 'DNS'." });
        }

        var result = new Result
        {
            EnrolmentId = enrolmentId,
            FinishTime = request.FinishTime,
            FinishPosition = request.FinishPosition,
            CategoryPosition = request.CategoryPosition,
            Status = request.Status,
            RecordedDate = DateTime.UtcNow
        };

        _db.Results.Add(result);
        await _db.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, await ToDtoAsync(result.ResultId));
    }

    /// <summary>PUT /api/results/{id} - corrects a previously captured result.</summary>
    [HttpPut("api/results/{id:int}")]
    [RequireRole(Role.Organiser)]
    public async Task<ActionResult<ResultDto>> Update(int id, CreateResultRequest request)
    {
        var result = await _db.Results
            .Include(r => r.Enrolment).ThenInclude(en => en.Event)
            .FirstOrDefaultAsync(r => r.ResultId == id);

        if (result is null) return NotFound();
        if (result.Enrolment.Event.OrganiserId != CurrentUserId)
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "You do not own this event." });

        var validStatuses = new[] { "Finished", "DNF", "DNS" };
        if (!validStatuses.Contains(request.Status))
        {
            return BadRequest(new { message = "Status must be 'Finished', 'DNF' or 'DNS'." });
        }

        result.FinishTime = request.FinishTime;
        result.FinishPosition = request.FinishPosition;
        result.CategoryPosition = request.CategoryPosition;
        result.Status = request.Status;

        await _db.SaveChangesAsync();
        return Ok(await ToDtoAsync(result.ResultId));
    }

    /// <summary>GET /api/events/{eventId}/results - public leaderboard.</summary>
    [HttpGet("api/events/{eventId:int}/results")]
    public async Task<ActionResult<IEnumerable<ResultDto>>> GetForEvent(int eventId)
    {
        var eventExists = await _db.Events.AnyAsync(e => e.EventId == eventId);
        if (!eventExists) return NotFound();

        var results = await _db.Results
            .Include(r => r.Enrolment).ThenInclude(en => en.Event)
            .Include(r => r.Enrolment).ThenInclude(en => en.Participant)
            .Where(r => r.Enrolment.EventId == eventId)
            .OrderBy(r => r.FinishPosition)
            .ToListAsync();

        return Ok(results.Select(ToDto));
    }

    /// <summary>GET /api/users/me/results - the logged-in Participant's own performance history.</summary>
    [HttpGet("api/users/me/results")]
    [RequireRole(Role.Participant)]
    public async Task<ActionResult<IEnumerable<ResultDto>>> GetMine()
    {
        var results = await _db.Results
            .Include(r => r.Enrolment).ThenInclude(en => en.Event)
            .Include(r => r.Enrolment).ThenInclude(en => en.Participant)
            .Where(r => r.Enrolment.ParticipantId == CurrentUserId)
            .ToListAsync();

        return Ok(results.Select(ToDto));
    }

    private async Task<ResultDto> ToDtoAsync(int resultId)
    {
        var result = await _db.Results
            .Include(r => r.Enrolment).ThenInclude(en => en.Event)
            .Include(r => r.Enrolment).ThenInclude(en => en.Participant)
            .FirstAsync(r => r.ResultId == resultId);

        return ToDto(result);
    }

    private static ResultDto ToDto(Result r) => new()
    {
        ResultId = r.ResultId,
        EnrolmentId = r.EnrolmentId,
        EventId = r.Enrolment.EventId,
        EventName = r.Enrolment.Event.EventName,
        ParticipantName = r.Enrolment.Participant.FullName,
        FinishTime = r.FinishTime,
        FinishPosition = r.FinishPosition,
        CategoryPosition = r.CategoryPosition,
        Status = r.Status,
        RecordedDate = r.RecordedDate
    };
}
