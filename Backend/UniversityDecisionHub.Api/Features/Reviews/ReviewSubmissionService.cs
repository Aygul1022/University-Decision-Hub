using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Globalization;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using UniversityDecisionHub.Api.Data;
using UniversityDecisionHub.Api.Features.Reviews.Dtos;
namespace UniversityDecisionHub.Api.Features.Reviews;

public record ReviewSubmissionResult(int StatusCode, string? Error = null, ReviewCreatedDto? Review = null);

public interface IReviewSubmissionService
{
    Task<ReviewSubmissionResult> SubmitAsync(ClaimsPrincipal principal, ReviewCreateDto input, CancellationToken ct);
}

public class ReviewSubmissionService(ApplicationDbContext db, TimeProvider clock) : IReviewSubmissionService
{
    public async Task<ReviewSubmissionResult> SubmitAsync(ClaimsPrincipal principal, ReviewCreateDto input, CancellationToken ct)
    {
        // Read only claims on an authenticated identity. Ozan must map the trusted subject to UserId.
        var identity = principal.Identities.FirstOrDefault(i => i.IsAuthenticated);
        var subjects = identity?.FindAll(ClaimTypes.NameIdentifier).Select(c => c.Value).Distinct().ToArray();
        if (subjects is not { Length: 1 } || !int.TryParse(subjects[0], NumberStyles.None, CultureInfo.InvariantCulture, out var userId) || userId <= 0)
            return new(401, "An authenticated account with a valid user ID is required.");
        if (input is null || !Validator.TryValidateObject(input, new ValidationContext(input), [], true))
            return new(400, "Provide positive course, offering and instructor IDs and nonblank review text.");

        try
        {
            // Keeps eligibility/profile/context checks and insertion in one consistent transaction.
            await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            var user = await db.UserAccounts.AsNoTracking().SingleOrDefaultAsync(u => u.UserId == userId, ct);
            if (user is null) return new(401, "The authenticated account no longer exists.");
            var course = await db.Courses.AsNoTracking().SingleOrDefaultAsync(c => c.CourseId == input.CourseId, ct);
            if (course is null) return new(404, "Course does not exist.");
            var eligibility = ReviewEligibility.Check(user.DepartmentId, user.StudyYear, course.DepartmentId, course.RecommendedStudyYear);
            if (eligibility is not null) return new(403, eligibility);
            if (!await db.CourseOfferings.AnyAsync(o => o.OfferingId == input.OfferingId && o.CourseId == input.CourseId, ct))
                return new(400, "The offering does not belong to the selected course.");
            if (!await db.OfferingInstructors.AnyAsync(a => a.OfferingId == input.OfferingId && a.InstructorId == input.InstructorId, ct))
                return new(400, "The instructor is not assigned to this offering.");
            if (await db.Reviews.AnyAsync(r => r.UserId == userId && r.CourseId == input.CourseId, ct))
                return new(409, "You have already reviewed this course.");

            var row = new Review { UserId = userId, CourseId = input.CourseId, OfferingId = input.OfferingId,
                InstructorId = input.InstructorId, ReviewText = input.ReviewText.Trim(), CreatedAt = clock.GetUtcNow().UtcDateTime };
            db.Reviews.Add(row);
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return new(201, Review: new ReviewCreatedDto(row.ReviewId, row.UserId, row.CourseId, row.OfferingId,
                row.InstructorId, row.ReviewText, row.CreatedAt));
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505", ConstraintName: "IX_Reviews_UserId_CourseId" })
        { return new(409, "You have already reviewed this course."); }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23503" })
        { return new(409, "The account or teaching context changed. Reload before submitting again."); }
        catch (Exception ex) when (ex is PostgresException { SqlState: "40001" or "40P01" }
            || ex.InnerException is PostgresException { SqlState: "40001" or "40P01" })
        { return new(409, "The data changed concurrently. Reload before submitting again."); }
    }
}
