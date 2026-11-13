using UniversityDecisionHub.Api.Features.Reviews.Dtos;

namespace UniversityDecisionHub.Api.Features.Reviews;

// Shared, provider-independent query composition: works on EF Core IQueryable
// and on in-memory IQueryable for behavior checks without a real database.
public static class ReviewReadFiltering
{
    public static IQueryable<ReviewReadDto> Apply(IQueryable<ReviewReadDto> source, ReviewReadQuery search)
    {
        if (!string.IsNullOrWhiteSpace(search.CourseCode))
        {
            // Partial, case-insensitive course-code match.
            var courseCode = search.CourseCode.Trim().ToUpperInvariant();
            source = source.Where(r => r.CourseCode.ToUpper().Contains(courseCode));
        }

        if (search.DepartmentId.HasValue)
            source = source.Where(r => r.DepartmentId == search.DepartmentId.Value);

        if (search.InstructorId.HasValue)
            source = source.Where(r => r.InstructorId == search.InstructorId.Value);

        if (!string.IsNullOrWhiteSpace(search.AcademicYear))
        {
            var academicYear = search.AcademicYear.Trim();
            source = source.Where(r => r.AcademicYear == academicYear);
        }

        return source;
    }

    public static IQueryable<ReviewReadDto> Page(IQueryable<ReviewReadDto> source, ReviewReadQuery search) =>
        source.OrderByDescending(r => r.CreatedAt)
              .ThenByDescending(r => r.ReviewId)
              .Skip((search.Page - 1) * search.PageSize)
              .Take(search.PageSize);
}
