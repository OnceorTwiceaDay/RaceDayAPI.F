namespace RaceDay.Api.Models;

/// <summary>ERD entity: Category. An age/distance division within an Event.</summary>
public class Category
{
    public int CategoryId { get; set; }
    public int EventId { get; set; }
    public string CategoryName { get; set; } = string.Empty; // e.g. "21.1km Half Marathon"
    public decimal DistanceKm { get; set; }
    public int MinAge { get; set; }
    public int? MaxAge { get; set; }
    public decimal EntryFee { get; set; }

    // Navigation properties
    public Event Event { get; set; } = null!;
    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
}
