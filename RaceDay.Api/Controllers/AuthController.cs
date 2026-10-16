using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Models;
using RaceDay.Contracts;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly RaceDayDbContext _db;
    private readonly PasswordHasher<AppUser> _hasher = new();

    public AuthController(RaceDayDbContext db)
    {
        _db = db;
    }

    /// <summary>POST /api/auth/register - creates an Organiser or Participant account.</summary>
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FullName) ||
            string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { message = "FullName, Email and Password are required." });
        }

        if (!Enum.TryParse<Role>(request.Role, ignoreCase: true, out var role))
        {
            return BadRequest(new { message = "Role must be either 'Organiser' or 'Participant'." });
        }

        var emailExists = await _db.Users.AnyAsync(u => u.Email == request.Email);
        if (emailExists)
        {
            return Conflict(new { message = "An account with this email already exists." });
        }

        var user = new AppUser
        {
            FullName = request.FullName,
            Email = request.Email,
            Role = role,
            PhoneNumber = request.PhoneNumber,
            DateOfBirth = request.DateOfBirth,
            RegisteredDate = DateTime.UtcNow
        };

        // Never store the raw password - hash it first.
        user.PasswordHash = _hasher.HashPassword(user, request.Password);

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var response = new AuthResponse
        {
            UserId = user.UserId,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role.ToString()
        };

        return StatusCode(StatusCodes.Status201Created, response);
    }

    /// <summary>POST /api/auth/login - verifies credentials and starts a server-side session.</summary>
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user is null)
        {
            return Unauthorized(new { message = "Invalid email or password." });
        }

        var verification = _hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (verification == PasswordVerificationResult.Failed)
        {
            return Unauthorized(new { message = "Invalid email or password." });
        }

        // This is the session Part 2 requires - not a JWT. Every subsequent
        // protected request is authorised by RequireRoleAttribute reading
        // these two session values.
        HttpContext.Session.SetInt32("UserId", user.UserId);
        HttpContext.Session.SetString("Role", user.Role.ToString());

        return Ok(new AuthResponse
        {
            UserId = user.UserId,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role.ToString()
        });
    }

    /// <summary>POST /api/auth/logout - clears the session.</summary>
    [HttpPost("logout")]
    public IActionResult Logout()
    {
        if (HttpContext.Session.GetInt32("UserId") is null)
        {
            return Unauthorized(new { message = "No active session." });
        }

        HttpContext.Session.Clear();
        return Ok(new { message = "Logged out successfully." });
    }
}
