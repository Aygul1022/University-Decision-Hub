using System.ComponentModel.DataAnnotations;
namespace UniversityDecisionHub.Api.Features.Offerings;
// Period/status vocabulary is not fixed yet; store administrator-supplied nonblank values.
public class OfferingInput
{
    [Required, StringLength(30)] public string AcademicYear { get; set; } = string.Empty;
    [Required, StringLength(30)] public string Semester { get; set; } = string.Empty;
    [Required, StringLength(30)] public string Status { get; set; } = string.Empty;
}
public class OfferingCreate : OfferingInput
{
    [Range(1, int.MaxValue)] public int CourseId { get; set; }
}
public class AssignmentInput
{
    [Range(1, int.MaxValue)] public int InstructorId { get; set; }
}
