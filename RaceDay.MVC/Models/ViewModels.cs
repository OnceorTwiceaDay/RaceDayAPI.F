using System.ComponentModel.DataAnnotations;
using RaceDay.Contracts;

namespace RaceDay.MVC.Models;

public class LoginViewModel
{
    [Required, EmailAddress] public string Email { get; set; } = "";
    [Required, DataType(DataType.Password)] public string Password { get; set; } = "";
}

public class RegisterViewModel
{
    [Required] public string FullName { get; set; } = "";
    [Required, EmailAddress] public string Email { get; set; } = "";
    [Required, MinLength(6), DataType(DataType.Password)] public string Password { get; set; } = "";
    [Required, RegularExpression("^(Participant|Organiser)$")] public string Role { get; set; } = "Participant";
}

public class EventDetailsViewModel
{
    public EventDto Event { get; set; } = new();
    public List<CategoryDto> Categories { get; set; } = new();
    public List<RouteDto> Routes { get; set; } = new();
    public List<ResultDto> Results { get; set; } = new();
}

public class OrganiserEventViewModel
{
    public EventDto Event { get; set; } = new();
    public List<EnrolmentDto> Enrolments { get; set; } = new();
    public List<CategoryDto> Categories { get; set; } = new();
    public List<ResultDto> Results { get; set; } = new();
}

public class OrganiserDashboardViewModel
{
    public List<EventDto> Events { get; set; } = new();
    public Dictionary<int, int> EntrantsByEvent { get; set; } = new();
}

public class ResultEntryViewModel
{
    public int EventId { get; set; }
    public int EnrolmentId { get; set; }
    public string Status { get; set; } = "Finished";
    public TimeSpan? FinishTime { get; set; }
    public int? FinishPosition { get; set; }
    public int? CategoryPosition { get; set; }
}

public class ProfilePageViewModel
{
    public ProfileDto Profile { get; set; } = new();
    public IFormFile? Image { get; set; }
}

public class EventFormViewModel
{
    [Required] public string EventName { get; set; } = "";
    public string? Description { get; set; }
    [Required] public DateOnly EventDate { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddDays(7));
    [Required] public string Location { get; set; } = "";
    [Range(0.01, 9999)] public decimal DistanceKm { get; set; }
    [Required] public string EventType { get; set; } = "Running";
    public IFormFile? Banner { get; set; }

    public CreateEventRequest ToRequest() => new()
    {
        EventName = EventName, Description = Description, EventDate = EventDate,
        Location = Location, DistanceKm = DistanceKm, EventType = EventType
    };
}
