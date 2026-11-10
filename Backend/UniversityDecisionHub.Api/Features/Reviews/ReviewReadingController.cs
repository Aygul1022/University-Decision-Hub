using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniversityDecisionHub.Api.Data;
using UniversityDecisionHub.Api.Features.Reviews.Dtos;

namespace UniversityDecisionHub.Api.Features.Reviews;

// Separate from Aygül's ReviewSubmissionController: no POST route, no shared-model edits.
// Ozan's authentication middleware must be integrated before these endpoints can be exercised.
[ApiController]
[Route("api/reviews")]
[Authorize]
public sealed class ReviewReadingController(ApplicationDbContext db) : ControllerBase
{
    // GET /api/reviews?courseCode=COME&departmentId=1&instructorId=2&academicYear=2026-2027&page=1&pageSize=20
    [HttpGet]
    public async Task<ActionResult<PagedReviewsDto>> List([FromQuery] ReviewReadQuery search, CancellationToken ct)
    {
        var query = ProjectReviews();

        if (!string.IsNullOrWhiteSpace(search.CourseCode))
        {
            // Partial, case-insensitive course-code match. Other filters use exact values.
            var courseCode = search.CourseCode.Trim().ToUpperInvariant();
            query = query.Where(r => r.CourseCode.ToUpper().Contains(courseCode));
        }

        if (search.DepartmentId.HasValue)
            query = query.Where(r => r.DepartmentId == search.DepartmentId.Value);

        if (search.InstructorId.HasValue)
            query = query.Where(r => r.InstructorId == search.InstructorId.Value);

        if (!string.IsNullOrWhiteSpace(search.AcademicYear))
            query = query.Where(r => r.AcademicYear == search.AcademicYear.Trim());

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .ThenByDescending(r => r.ReviewId)
            .Skip((search.Page - 1) * search.PageSize)
            .Take(search.PageSize)
            .ToListAsync(ct);

        // A valid search with no matches is not an error.
        return Ok(new PagedReviewsDto
        {
            Page = search.Page,
            PageSize = search.PageSize,
            TotalCount = totalCount,
            Items = items
        });
    }

    // GET /api/reviews/123 — full review text still requires a logged-in user.
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ReviewReadDto>> Detail(int id, CancellationToken ct)
    {
        if (id <= 0) return BadRequest("Review id must be positive.");

        var review = await ProjectReviews().FirstOrDefaultAsync(r => r.ReviewId == id, ct);
        return review is null ? NotFound() : Ok(review);
    }

    private IQueryable<ReviewReadDto> ProjectReviews()
    {
        // Explicit joins avoid introducing navigation properties or editing team-owned entities.
        return from review in db.Reviews.AsNoTracking()
               join course in db.Courses.AsNoTracking() on review.CourseId equals course.CourseId
               join offering in db.CourseOfferings.AsNoTracking() on review.OfferingId equals offering.OfferingId
               join instructor in db.Instructors.AsNoTracking() on review.InstructorId equals instructor.InstructorId
               select new ReviewReadDto
               {
                   ReviewId = review.ReviewId,
                   CourseId = review.CourseId,
                   CourseCode = course.CourseCode,
                   CourseName = course.CourseName,
                   DepartmentId = course.DepartmentId,
                   OfferingId = review.OfferingId,
                   AcademicYear = offering.AcademicYear,
                   Semester = offering.Semester,
                   InstructorId = instructor.InstructorId,
                   InstructorName = instructor.FullName,
                   ReviewText = review.ReviewText,
                   CreatedAt = review.CreatedAt
               };
    }
}
