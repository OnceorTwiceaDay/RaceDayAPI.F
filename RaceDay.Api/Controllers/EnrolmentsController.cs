using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Filters;
using RaceDay.Api.Models;
using RaceDay.Contracts;

namespace RaceDay.Api.Controllers;

[ApiController]
public class EnrolmentsController : RaceDayControllerBase
{
    private readonly RaceDayDbContext _db;

    public EnrolmentsController(RaceDayDbContext db)
    {
        _db = db;
    }

    /// <summary>POST /api/events/{eventId}/enrolments - Participant enters an event under a category.</summary>
    [HttpPost("api/events/{eventId:int}/enrolments")]
    [RequireRole(Role.Participant)]
    public async Task<ActionResult<EnrolmentDto>> Enrol(int eventId, CreateEnrolmentRequest request)
    {
        var ev = await _db.Events.FirstOrDefaultAsync(e => e.EventId == eventId);
        if (ev is null) return NotFound(new { message = "Event not found." });

        var category = await _db.Categories.FirstOrDefaultAsync(c => c.CategoryId == request.CategoryId && c.EventId == eventId);
        if (category is null) return NotFound(new { message = "Category not found for this event." });

        var alreadyEnrolled = await _db.Enrolments.AnyAsync(en => en.ParticipantId == CurrentUserId && en.EventId == eventId);
        if (alreadyEnrolled)
        {
            return Conflict(new { message = "You are already enrolled in this event." });
        }

        var enrolment = new Enrolment
        {
            ParticipantId = CurrentUserId,
            EventId = eventId,
            CategoryId = request.CategoryId,
            EnrolmentDate = DateTime.UtcNow,
            Status = "Confirmed",
            PaymentStatus = "Unpaid"
        };

        _db.Enrolments.Add(enrolment);
        await _db.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, await ToDtoAsync(enrolment.EnrolmentId));
    }

    /// <summary>GET /api/users/me/enrolments - the logged-in Participant's own enrolment history.</summary>
    [HttpGet("api/users/me/enrolments")]
    [RequireRole(Role.Participant)]
    public async Task<ActionResult<IEnumerable<EnrolmentDto>>> GetMine()
    {
        var enrolments = await _db.Enrolments
            .Include(en => en.Participant)
            .Include(en => en.Event)
            .Include(en => en.Category)
            .Where(en => en.ParticipantId == CurrentUserId)
            .ToListAsync();

        return Ok(enrolments.Select(ToDto));
    }

    /// <summary>GET /api/events/{eventId}/enrolments - the Organiser who owns the event views its entrants.</summary>
    [HttpGet("api/events/{eventId:int}/enrolments")]
    [RequireRole(Role.Organiser)]
    public async Task<ActionResult<IEnumerable<EnrolmentDto>>> GetForEvent(int eventId)
    {
        var ev = await _db.Events.FirstOrDefaultAsync(e => e.EventId == eventId);
        if (ev is null) return NotFound();
        if (ev.OrganiserId != CurrentUserId)
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "You do not own this event." });

        var enrolments = await _db.Enrolments
            .Include(en => en.Participant)
            .Include(en => en.Event)
            .Include(en => en.Category)
            .Where(en => en.EventId == eventId)
            .ToListAsync();

        return Ok(enrolments.Select(ToDto));
    }

    /// <summary>
    /// PUT /api/enrolments/{id}/status - Organiser confirms/cancels an entrant
    /// (e.g. once payment is received). Added during Part 2, matching the Part 2
    /// brief's own suggested status-management endpoint; documented in the
    /// README as a deliberate addition to the Part 1 plan.
    /// </summary>
    [HttpPut("api/enrolments/{id:int}/status")]
    [RequireRole(Role.Organiser)]
    public async Task<ActionResult<EnrolmentDto>> UpdateStatus(int id, UpdateEnrolmentStatusRequest request)
    {
        var enrolment = await _db.Enrolments.Include(en => en.Event).FirstOrDefaultAsync(en => en.EnrolmentId == id);
        if (enrolment is null) return NotFound();
        if (enrolment.Event.OrganiserId != CurrentUserId)
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "You do not own this event." });

        var validStatuses = new[] { "Pending", "Confirmed", "Cancelled" };
        if (!validStatuses.Contains(request.Status))
        {
            return BadRequest(new { message = "Status must be 'Pending', 'Confirmed' or 'Cancelled'." });
        }

        enrolment.Status = request.Status;
        await _db.SaveChangesAsync();

        return Ok(await ToDtoAsync(enrolment.EnrolmentId));
    }

    /// <summary>DELETE /api/enrolments/{id} - a Participant cancels their own enrolment.</summary>
    [HttpDelete("api/enrolments/{id:int}")]
    [RequireRole(Role.Participant)]
    public async Task<IActionResult> Cancel(int id)
    {
        var enrolment = await _db.Enrolments.Include(en => en.Event).FirstOrDefaultAsync(en => en.EnrolmentId == id);
        if (enrolment is null) return NotFound();
        if (enrolment.ParticipantId != CurrentUserId)
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "This is not your enrolment." });

        if (enrolment.Event.EventDate <= DateOnly.FromDateTime(DateTime.UtcNow))
        {
            return Conflict(new { message = "This event has already started; the enrolment can no longer be cancelled." });
        }

        _db.Enrolments.Remove(enrolment);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private async Task<EnrolmentDto> ToDtoAsync(int enrolmentId)
    {
        var enrolment = await _db.Enrolments
            .Include(en => en.Participant)
            .Include(en => en.Event)
            .Include(en => en.Category)
            .FirstAsync(en => en.EnrolmentId == enrolmentId);

        return ToDto(enrolment);
    }

    private static EnrolmentDto ToDto(Enrolment en) => new()
    {
        EnrolmentId = en.EnrolmentId,
        ParticipantId = en.ParticipantId,
        ParticipantName = en.Participant.FullName,
        EventId = en.EventId,
        EventName = en.Event.EventName,
        CategoryId = en.CategoryId,
        CategoryName = en.Category.CategoryName,
        EnrolmentDate = en.EnrolmentDate,
        Status = en.Status,
        PaymentStatus = en.PaymentStatus
    };
}
