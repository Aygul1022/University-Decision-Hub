using System.ComponentModel.DataAnnotations;

namespace UniversityDecisionHub.Api.Features.Reviews.Dtos;

// Parameters for Sami's read-only API. All supplied filters are combined with AND.
public sealed class ReviewReadQuery
{
    [MaxLength(64)]
    public string? CourseCode { get; set; }

    [Range(1, int.MaxValue)]
    public int? DepartmentId { get; set; }

    [Range(1, int.MaxValue)]
    public int? InstructorId { get; set; }

    [RegularExpression(@"^[0-9]{4}-[0-9]{4}$")]
    public string? AcademicYear { get; set; }

    [RegularExpression("^(Fall|Spring|Summer)$")]
    public string? Semester { get; set; }

    [Range(1, 100000)]
    public int Page { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 20;
}
