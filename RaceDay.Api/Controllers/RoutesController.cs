using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Filters;
using RaceDay.Api.Models;
using RaceDay.Contracts;

namespace RaceDay.Api.Controllers;

[ApiController]
public class RoutesController : RaceDayControllerBase
{
    private readonly RaceDayDbContext _db;

    public RoutesController(RaceDayDbContext db)
    {
        _db = db;
    }

    /// <summary>GET /api/events/{eventId}/routes - public, so participants can prepare for race day.</summary>
    [HttpGet("api/events/{eventId:int}/routes")]
    public async Task<ActionResult<IEnumerable<RouteDto>>> GetForEvent(int eventId)
    {
        var eventExists = await _db.Events.AnyAsync(e => e.EventId == eventId);
        if (!eventExists) return NotFound();

        var routes = await _db.Routes.Where(r => r.EventId == eventId).ToListAsync();
        return Ok(routes.Select(ToDto));
    }

    /// <summary>POST /api/events/{eventId}/routes - only the Organiser who owns the event.</summary>
    [HttpPost("api/events/{eventId:int}/routes")]
    [RequireRole(Role.Organiser)]
    public async Task<ActionResult<RouteDto>> Create(int eventId, CreateRouteRequest request)
    {
        var ev = await _db.Events.FirstOrDefaultAsync(e => e.EventId == eventId);
        if (ev is null) return NotFound();
        if (ev.OrganiserId != CurrentUserId)
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "You do not own this event." });

        if (string.IsNullOrWhiteSpace(request.RouteName) || request.DistanceKm <= 0 ||
            string.IsNullOrWhiteSpace(request.StartPoint) || string.IsNullOrWhiteSpace(request.EndPoint))
        {
            return BadRequest(new { message = "RouteName, a positive DistanceKm, StartPoint and EndPoint are required." });
        }

        var route = new RaceRoute
        {
            EventId = eventId,
            RouteName = request.RouteName,
            DistanceKm = request.DistanceKm,
            StartPoint = request.StartPoint,
            EndPoint = request.EndPoint,
            ElevationGainM = request.ElevationGainM,
            MapUrl = request.MapUrl
        };

        _db.Routes.Add(route);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetForEvent), new { eventId }, ToDto(route));
    }

    private static RouteDto ToDto(RaceRoute r) => new()
    {
        RouteId = r.RouteId,
        EventId = r.EventId,
        RouteName = r.RouteName,
        DistanceKm = r.DistanceKm,
        StartPoint = r.StartPoint,
        EndPoint = r.EndPoint,
        ElevationGainM = r.ElevationGainM,
        MapUrl = r.MapUrl
    };
}
