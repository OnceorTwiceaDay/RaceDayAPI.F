namespace RaceDay.Api.Models;

/// <summary>
/// The two RaceDay user roles. Stored on AppUser as a string column
/// (see RaceDayDbContext), matching the CHECK constraint on Users.Role
/// in RaceDay_Database.sql.
/// </summary>
public enum Role
{
    Organiser,
    Participant
}
