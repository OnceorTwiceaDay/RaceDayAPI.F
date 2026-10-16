namespace RaceDay.Contracts;

// =====================================================================
// Authentication
// =====================================================================

public class RegisterRequest
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty; // "Organiser" or "Participant"
    public string? PhoneNumber { get; set; }
    public DateOnly? DateOfBirth { get; set; }
}

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// Returned on successful register/login. No token here - the session
/// cookie set by ASP.NET Core is what keeps the user authenticated.
/// </summary>
public class AuthResponse
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

// =====================================================================
// User Profile
// =====================================================================

public class ProfileDto
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public DateTime RegisteredDate { get; set; }
}

public class UpdateProfileRequest
{
    public string FullName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public DateOnly? DateOfBirth { get; set; }
}

// =====================================================================
// Events
// =====================================================================

public class CreateEventRequest
{
    public string EventName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateOnly EventDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public decimal DistanceKm { get; set; }
    public string EventType { get; set; } = string.Empty; // "Running" | "Walking" | "Cycling"
}

public class UpdateEventRequest : CreateEventRequest { }

public class EventDto
{
    public int EventId { get; set; }
    public int OrganiserId { get; set; }
    public string OrganiserName { get; set; } = string.Empty;
    public string EventName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateOnly EventDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public decimal DistanceKm { get; set; }
    public string EventType { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
}

// =====================================================================
// Categories
// =====================================================================

public class CreateCategoryRequest
{
    public string CategoryName { get; set; } = string.Empty;
    public decimal DistanceKm { get; set; }
    public int MinAge { get; set; }
    public int? MaxAge { get; set; }
    public decimal EntryFee { get; set; }
}

public class UpdateCategoryRequest : CreateCategoryRequest { }

public class CategoryDto
{
    public int CategoryId { get; set; }
    public int EventId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal DistanceKm { get; set; }
    public int MinAge { get; set; }
    public int? MaxAge { get; set; }
    public decimal EntryFee { get; set; }
}

// =====================================================================
// Routes
// =====================================================================

public class CreateRouteRequest
{
    public string RouteName { get; set; } = string.Empty;
    public decimal DistanceKm { get; set; }
    public string StartPoint { get; set; } = string.Empty;
    public string EndPoint { get; set; } = string.Empty;
    public int? ElevationGainM { get; set; }
    public string? MapUrl { get; set; }
}

public class RouteDto
{
    public int RouteId { get; set; }
    public int EventId { get; set; }
    public string RouteName { get; set; } = string.Empty;
    public decimal DistanceKm { get; set; }
    public string StartPoint { get; set; } = string.Empty;
    public string EndPoint { get; set; } = string.Empty;
    public int? ElevationGainM { get; set; }
    public string? MapUrl { get; set; }
}

// =====================================================================
// Event Enrolments
// =====================================================================

public class CreateEnrolmentRequest
{
    public int CategoryId { get; set; }
}

public class UpdateEnrolmentStatusRequest
{
    public string Status { get; set; } = string.Empty; // "Pending" | "Confirmed" | "Cancelled"
}

public class EnrolmentDto
{
    public int EnrolmentId { get; set; }
    public int ParticipantId { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public DateTime EnrolmentDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
}

// =====================================================================
// Results
// =====================================================================

public class CreateResultRequest
{
    public int EnrolmentId { get; set; }
    public TimeSpan? FinishTime { get; set; }
    public int? FinishPosition { get; set; }
    public int? CategoryPosition { get; set; }
    public string Status { get; set; } = "Finished"; // "Finished" | "DNF" | "DNS"
}

public class ResultDto
{
    public int ResultId { get; set; }
    public int EnrolmentId { get; set; }
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public string ParticipantName { get; set; } = string.Empty;
    public TimeSpan? FinishTime { get; set; }
    public int? FinishPosition { get; set; }
    public int? CategoryPosition { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime RecordedDate { get; set; }
}
