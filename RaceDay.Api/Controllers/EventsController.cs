using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Filters;
using RaceDay.Api.Models;
using RaceDay.Contracts;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/events")]
public class EventsController : RaceDayControllerBase
{
    private readonly RaceDayDbContext _db;

    public EventsController(RaceDayDbContext db)
    {
        _db = db;
    }

    /// <summary>GET /api/events - public list of all events.</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<EventDto>>> GetAll()
    {
        var events = await _db.Events.Include(e => e.Organiser).ToListAsync();
        return Ok(events.Select(ToDto));
    }

    /// <summary>GET /api/events/{id} - public single event.</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<EventDto>> GetById(int id)
    {
        var ev = await _db.Events.Include(e => e.Organiser).FirstOrDefaultAsync(e => e.EventId == id);
        if (ev is null) return NotFound();
        return Ok(ToDto(ev));
    }

    /// <summary>
    /// GET /api/events/mine - events owned by the logged-in Organiser. Added
    /// during Part 2 to match the Part 2 brief's explicit endpoint list;
    /// documented as a deliberate addition to the Part 1 plan in the README.
    /// </summary>
    [HttpGet("mine")]
    [RequireRole(Role.Organiser)]
    public async Task<ActionResult<IEnumerable<EventDto>>> GetMine()
    {
        var events = await _db.Events
            .Include(e => e.Organiser)
            .Where(e => e.OrganiserId == CurrentUserId)
            .ToListAsync();

        return Ok(events.Select(ToDto));
    }

    /// <summary>POST /api/events - Organiser only.</summary>
    [HttpPost]
    [RequireRole(Role.Organiser)]
    public async Task<ActionResult<EventDto>> Create(CreateEventRequest request)
    {
        if (!IsValid(request, out var error)) return BadRequest(new { message = error });

        var ev = new Event
        {
            OrganiserId = CurrentUserId,
            EventName = request.EventName,
            Description = request.Description,
            EventDate = request.EventDate,
            Location = request.Location,
            DistanceKm = request.DistanceKm,
            EventType = request.EventType,
            CreatedDate = DateTime.UtcNow
        };

        _db.Events.Add(ev);
        await _db.SaveChangesAsync();
        await _db.Entry(ev).Reference(e => e.Organiser).LoadAsync();

        return CreatedAtAction(nameof(GetById), new { id = ev.EventId }, ToDto(ev));
    }

    /// <summary>PUT /api/events/{id} - only the owning Organiser may edit.</summary>
    [HttpPut("{id:int}")]
    [RequireRole(Role.Organiser)]
    public async Task<ActionResult<EventDto>> Update(int id, UpdateEventRequest request)
    {
        var ev = await _db.Events.Include(e => e.Organiser).FirstOrDefaultAsync(e => e.EventId == id);
        if (ev is null) return NotFound();
        if (ev.OrganiserId != CurrentUserId)
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "You do not own this event." });

        if (!IsValid(request, out var error)) return BadRequest(new { message = error });

        ev.EventName = request.EventName;
        ev.Description = request.Description;
        ev.EventDate = request.EventDate;
        ev.Location = request.Location;
        ev.DistanceKm = request.DistanceKm;
        ev.EventType = request.EventType;

        await _db.SaveChangesAsync();
        return Ok(ToDto(ev));
    }

    /// <summary>DELETE /api/events/{id} - only the owning Organiser may delete.</summary>
    [HttpDelete("{id:int}")]
    [RequireRole(Role.Organiser)]
    public async Task<IActionResult> Delete(int id)
    {
        var ev = await _db.Events.FirstOrDefaultAsync(e => e.EventId == id);
        if (ev is null) return NotFound();
        if (ev.OrganiserId != CurrentUserId)
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "You do not own this event." });

        _db.Events.Remove(ev);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private static bool IsValid(CreateEventRequest request, out string error)
    {
        if (string.IsNullOrWhiteSpace(request.EventName)) { error = "EventName is required."; return false; }
        if (string.IsNullOrWhiteSpace(request.Location)) { error = "Location is required."; return false; }
        if (request.DistanceKm <= 0) { error = "DistanceKm must be greater than zero."; return false; }

        var validTypes = new[] { "Running", "Walking", "Cycling" };
        if (!validTypes.Contains(request.EventType))
        {
            error = "EventType must be 'Running', 'Walking' or 'Cycling'.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    private static EventDto ToDto(Event ev) => new()
    {
        EventId = ev.EventId,
        OrganiserId = ev.OrganiserId,
        OrganiserName = ev.Organiser.FullName,
        EventName = ev.EventName,
        Description = ev.Description,
        EventDate = ev.EventDate,
        Location = ev.Location,
        DistanceKm = ev.DistanceKm,
        EventType = ev.EventType,
        CreatedDate = ev.CreatedDate
    };
}
