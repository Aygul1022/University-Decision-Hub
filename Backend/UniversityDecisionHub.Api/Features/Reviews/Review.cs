namespace UniversityDecisionHub.Api.Features.Reviews;
public class Review
{
    public int ReviewId { get; set; }
    public int UserId { get; set; }
    // Controlled redundancy supports the unique user/course rule across all offerings.
    public int CourseId { get; set; }
    public int OfferingId { get; set; }
    public int InstructorId { get; set; }
    public string ReviewText { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
