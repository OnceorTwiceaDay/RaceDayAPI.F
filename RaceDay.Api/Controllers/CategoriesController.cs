using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Filters;
using RaceDay.Api.Models;
using RaceDay.Contracts;

namespace RaceDay.Api.Controllers;

[ApiController]
public class CategoriesController : RaceDayControllerBase
{
    private readonly RaceDayDbContext _db;

    public CategoriesController(RaceDayDbContext db)
    {
        _db = db;
    }

    /// <summary>GET /api/events/{eventId}/categories - public.</summary>
    [HttpGet("api/events/{eventId:int}/categories")]
    public async Task<ActionResult<IEnumerable<CategoryDto>>> GetForEvent(int eventId)
    {
        var eventExists = await _db.Events.AnyAsync(e => e.EventId == eventId);
        if (!eventExists) return NotFound();

        var categories = await _db.Categories.Where(c => c.EventId == eventId).ToListAsync();
        return Ok(categories.Select(ToDto));
    }

    /// <summary>POST /api/events/{eventId}/categories - only the Organiser who owns the event.</summary>
    [HttpPost("api/events/{eventId:int}/categories")]
    [RequireRole(Role.Organiser)]
    public async Task<ActionResult<CategoryDto>> Create(int eventId, CreateCategoryRequest request)
    {
        var ev = await _db.Events.FirstOrDefaultAsync(e => e.EventId == eventId);
        if (ev is null) return NotFound();
        if (ev.OrganiserId != CurrentUserId)
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "You do not own this event." });

        if (!IsValid(request, out var error)) return BadRequest(new { message = error });

        var category = new Category
        {
            EventId = eventId,
            CategoryName = request.CategoryName,
            DistanceKm = request.DistanceKm,
            MinAge = request.MinAge,
            MaxAge = request.MaxAge,
            EntryFee = request.EntryFee
        };

        _db.Categories.Add(category);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetForEvent), new { eventId }, ToDto(category));
    }

    /// <summary>PUT /api/categories/{id} - only the Organiser who owns the parent event.</summary>
    [HttpPut("api/categories/{id:int}")]
    [RequireRole(Role.Organiser)]
    public async Task<ActionResult<CategoryDto>> Update(int id, UpdateCategoryRequest request)
    {
        var category = await _db.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryId == id);
        if (category is null) return NotFound();
        if (category.Event.OrganiserId != CurrentUserId)
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "You do not own this event." });

        if (!IsValid(request, out var error)) return BadRequest(new { message = error });

        category.CategoryName = request.CategoryName;
        category.DistanceKm = request.DistanceKm;
        category.MinAge = request.MinAge;
        category.MaxAge = request.MaxAge;
        category.EntryFee = request.EntryFee;

        await _db.SaveChangesAsync();
        return Ok(ToDto(category));
    }

    /// <summary>DELETE /api/categories/{id} - blocked (409) if participants are already enrolled.</summary>
    [HttpDelete("api/categories/{id:int}")]
    [RequireRole(Role.Organiser)]
    public async Task<IActionResult> Delete(int id)
    {
        var category = await _db.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryId == id);
        if (category is null) return NotFound();
        if (category.Event.OrganiserId != CurrentUserId)
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "You do not own this event." });

        var hasEnrolments = await _db.Enrolments.AnyAsync(e => e.CategoryId == id);
        if (hasEnrolments)
        {
            return Conflict(new { message = "This category has existing enrolments and cannot be deleted." });
        }

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private static bool IsValid(CreateCategoryRequest request, out string error)
    {
        if (string.IsNullOrWhiteSpace(request.CategoryName)) { error = "CategoryName is required."; return false; }
        if (request.DistanceKm <= 0) { error = "DistanceKm must be greater than zero."; return false; }
        if (request.MinAge < 0) { error = "MinAge cannot be negative."; return false; }
        if (request.MaxAge is not null && request.MaxAge < request.MinAge)
        {
            error = "MaxAge cannot be less than MinAge.";
            return false;
        }
        if (request.EntryFee < 0) { error = "EntryFee cannot be negative."; return false; }

        error = string.Empty;
        return true;
    }

    private static CategoryDto ToDto(Category c) => new()
    {
        CategoryId = c.CategoryId,
        EventId = c.EventId,
        CategoryName = c.CategoryName,
        DistanceKm = c.DistanceKm,
        MinAge = c.MinAge,
        MaxAge = c.MaxAge,
        EntryFee = c.EntryFee
    };
}
