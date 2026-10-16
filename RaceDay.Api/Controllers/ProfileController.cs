using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Data;
using RaceDay.Api.Filters;
using RaceDay.Contracts;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/users")]
public class ProfileController : RaceDayControllerBase
{
    private readonly RaceDayDbContext _db;

    public ProfileController(RaceDayDbContext db)
    {
        _db = db;
    }

    /// <summary>GET /api/users/me - any logged-in user's own profile.</summary>
    [HttpGet("me")]
    [RequireRole]
    public async Task<ActionResult<ProfileDto>> GetMyProfile()
    {
        var user = await _db.Users.FindAsync(CurrentUserId);
        if (user is null) return NotFound();

        return Ok(ToDto(user.UserId, user.FullName, user.Email, user.Role.ToString(),
                         user.PhoneNumber, user.DateOfBirth, user.RegisteredDate));
    }

    /// <summary>PUT /api/users/me - a user may only ever update their own profile (never by UserId).</summary>
    [HttpPut("me")]
    [RequireRole]
    public async Task<ActionResult<ProfileDto>> UpdateMyProfile(UpdateProfileRequest request)
    {
        var user = await _db.Users.FindAsync(CurrentUserId);
        if (user is null) return NotFound();

        if (string.IsNullOrWhiteSpace(request.FullName))
        {
            return BadRequest(new { message = "FullName is required." });
        }

        user.FullName = request.FullName;
        user.PhoneNumber = request.PhoneNumber;
        user.DateOfBirth = request.DateOfBirth;

        await _db.SaveChangesAsync();

        return Ok(ToDto(user.UserId, user.FullName, user.Email, user.Role.ToString(),
                         user.PhoneNumber, user.DateOfBirth, user.RegisteredDate));
    }

    private static ProfileDto ToDto(int id, string name, string email, string role,
        string? phone, DateOnly? dob, DateTime registered) => new()
    {
        UserId = id,
        FullName = name,
        Email = email,
        Role = role,
        PhoneNumber = phone,
        DateOfBirth = dob,
        RegisteredDate = registered
    };
}
