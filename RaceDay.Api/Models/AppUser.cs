namespace RaceDay.Api.Models;

/// <summary>
/// ERD entity: User. Named AppUser (not "User") to avoid any ambiguity
/// with ASP.NET Core's own ClaimsPrincipal-style "User" naming. Maps to
/// the "Users" table (plural, for the same reason documented in
/// RaceDay_Database.sql - USER is a reserved word in T-SQL).
/// </summary>
public class AppUser
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    /// <summary>Never store the raw password - only ever a hash. See AuthController.</summary>
    public string PasswordHash { get; set; } = string.Empty;

    public Role Role { get; set; }
    public string? PhoneNumber { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public DateTime RegisteredDate { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<Event> OrganisedEvents { get; set; } = new List<Event>();
    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
}
