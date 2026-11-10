namespace UniversityDecisionHub.Api.Features.Instructors;
public class Instructor
{
    public int InstructorId { get; set; }
    public int DepartmentId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? AcademicTitle { get; set; }
}
