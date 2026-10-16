namespace RaceDay.Api.Models;

/// <summary>
/// ERD entity: Route. Named RaceRoute (not "Route") purely to avoid any
/// ambiguity with Microsoft.AspNetCore.Routing.Route. The DbSet and the
/// database table are still called "Routes", matching the ERD and SQL
/// script exactly - this is a C# naming choice only, not a schema change.
/// </summary>
public class RaceRoute
{
    public int RouteId { get; set; }
    public int EventId { get; set; }
    public string RouteName { get; set; } = string.Empty;
    public decimal DistanceKm { get; set; }
    public string StartPoint { get; set; } = string.Empty;
    public string EndPoint { get; set; } = string.Empty;
    public int? ElevationGainM { get; set; }
    public string? MapUrl { get; set; }

    // Navigation property
    public Event Event { get; set; } = null!;
}
