namespace RaceDay.Api.Models;

/// <summary>
/// ERD entity: Enrolment. The associative entity that resolves the
/// many-to-many relationship between Participants and Events (via a
/// chosen Category).
/// </summary>
public class Enrolment
{
    public int EnrolmentId { get; set; }
    public int ParticipantId { get; set; }
    public int EventId { get; set; }
    public int CategoryId { get; set; }
    public DateTime EnrolmentDate { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "Confirmed";       // Pending | Confirmed | Cancelled
    public string PaymentStatus { get; set; } = "Unpaid";   // Unpaid | Paid | Refunded

    // Navigation properties
    public AppUser Participant { get; set; } = null!;
    public Event Event { get; set; } = null!;
    public Category Category { get; set; } = null!;

    /// <summary>0..1 - a result only exists once the Organiser captures it.</summary>
    public Result? Result { get; set; }
}
