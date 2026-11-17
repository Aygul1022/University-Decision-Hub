using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniversityDecisionHub.Api.Data;
using UniversityDecisionHub.Api.Features.Authentication;
using UniversityDecisionHub.Api.Features.Reviews.Dtos;

namespace UniversityDecisionHub.Api.Features.Reviews;

[ApiController]
[Route("api/reviews")]
[Authorize]
public sealed class ReviewReadingController(ApplicationDbContext db) : ControllerBase
{
    // GET /api/reviews?courseCode=COME&departmentId=1&instructorId=2&academicYear=2026-2027&semester=Fall
    [HttpGet]
    public async Task<ActionResult<PagedReviewsDto>> List(
        [FromQuery] ReviewReadQuery search, [FromQuery] bool includeHidden, CancellationToken ct)
    {
        if (includeHidden && !User.IsInRole(AuthRoles.Administrator)) return Forbid();

        var query = ReviewReadFiltering.Apply(ProjectReviews(includeHidden), search);

        var totalCount = await query.CountAsync(ct);
        var items = await ReviewReadFiltering.Page(query, search).ToListAsync(ct);

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

        var review = await ProjectReviews(User.IsInRole(AuthRoles.Administrator))
            .FirstOrDefaultAsync(r => r.ReviewId == id, ct);
        return review is null ? NotFound() : Ok(review);
    }

    private IQueryable<ReviewReadDto> ProjectReviews(bool includeHidden)
    {
        // Reviews with an offering can still get missing period fields from that legacy record.
        return from review in db.Reviews.AsNoTracking()
               where includeHidden || !review.IsHidden
               join course in db.Courses.AsNoTracking() on review.CourseId equals course.CourseId
               join instructor in db.Instructors.AsNoTracking() on review.InstructorId equals instructor.InstructorId
               join legacyOffering in db.CourseOfferings.AsNoTracking()
                   on review.OfferingId equals (int?)legacyOffering.OfferingId into legacyOfferings
               from offering in legacyOfferings.DefaultIfEmpty()
               select new ReviewReadDto
               {
                   ReviewId = review.ReviewId,
                   CourseId = review.CourseId,
                   CourseCode = course.CourseCode,
                   CourseName = course.CourseName,
                   DepartmentId = course.DepartmentId,
                   OfferingId = review.OfferingId,
                   AcademicYear = review.AcademicYear ?? (offering == null ? null : offering.AcademicYear),
                   Semester = review.Semester ?? (offering == null ? null : offering.Semester),
                   InstructorId = instructor.InstructorId,
                   InstructorName = instructor.FullName,
                   ReviewText = review.ReviewText,
                   CreatedAt = review.CreatedAt,
                   IsHidden = review.IsHidden
               };
    }
}
