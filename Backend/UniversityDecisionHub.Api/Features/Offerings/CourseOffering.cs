namespace UniversityDecisionHub.Api.Features.Offerings;
public class CourseOffering
{
    public int OfferingId { get; set; }
    public int CourseId { get; set; }
    public string AcademicYear { get; set; } = string.Empty;
    public string Semester { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}
public class OfferingInstructor
{
    public int OfferingId { get; set; }
    public int InstructorId { get; set; }
}
