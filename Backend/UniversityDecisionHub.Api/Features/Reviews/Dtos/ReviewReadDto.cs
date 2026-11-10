namespace UniversityDecisionHub.Api.Features.Reviews.Dtos;

// No author identity is exposed until the team approves its visibility policy.
// ReviewText is plain user text: frontend code must render it via textContent.
public sealed class ReviewReadDto
{
    public int ReviewId { get; set; }
    public int CourseId { get; set; }
    public string CourseCode { get; set; } = string.Empty;
    public string CourseName { get; set; } = string.Empty;
    public int DepartmentId { get; set; }
    public int OfferingId { get; set; }
    public string AcademicYear { get; set; } = string.Empty;
    public string Semester { get; set; } = string.Empty;
    public int InstructorId { get; set; }
    public string InstructorName { get; set; } = string.Empty;
    public string ReviewText { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public sealed class PagedReviewsDto
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public List<ReviewReadDto> Items { get; set; } = [];
}
