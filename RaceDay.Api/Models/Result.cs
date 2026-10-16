namespace RaceDay.Api.Models;

/// <summary>
/// ERD entity: Result. 1:0..1 with Enrolment - enforced in the database
/// via a UNIQUE constraint on EnrolmentId (see RaceDayDbContext).
/// </summary>
public class Result
{
    public int ResultId { get; set; }
    public int EnrolmentId { get; set; }
    public TimeSpan? FinishTime { get; set; }
    public int? FinishPosition { get; set; }
    public int? CategoryPosition { get; set; }
    public string Status { get; set; } = "Finished"; // Finished | DNF | DNS
    public DateTime RecordedDate { get; set; } = DateTime.UtcNow;

    // Navigation property
    public Enrolment Enrolment { get; set; } = null!;
}
