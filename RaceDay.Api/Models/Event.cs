namespace RaceDay.Api.Models;

/// <summary>ERD entity: Event.</summary>
public class Event
{
    public int EventId { get; set; }
    public int OrganiserId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateOnly EventDate { get; set; }
    public string Location { get; set; } = string.Empty;

    /// <summary>
    /// Added after Part 1 marker feedback: the Events table was missing the
    /// required distance field (also required explicitly by the Part 2 brief's
    /// Event model spec). Represents the event's flagship/headline distance.
    /// </summary>
    public decimal DistanceKm { get; set; }

    public string EventType { get; set; } = string.Empty; // "Running" | "Walking" | "Cycling"
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public AppUser Organiser { get; set; } = null!;
    public ICollection<Category> Categories { get; set; } = new List<Category>();
    public ICollection<RaceRoute> Routes { get; set; } = new List<RaceRoute>();
    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
}
