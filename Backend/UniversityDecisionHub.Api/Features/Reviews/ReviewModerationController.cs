using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniversityDecisionHub.Api.Data;
using UniversityDecisionHub.Api.Features.Authentication;
using UniversityDecisionHub.Api.Features.Reviews.Dtos;

namespace UniversityDecisionHub.Api.Features.Reviews;

[ApiController]
[Route("api/reviews")]
[Authorize(Policy = AuthPolicies.Administrator)]
public sealed class ReviewModerationController(ApplicationDbContext db, TimeProvider clock) : ControllerBase
{
    [HttpPut("{id:int}/moderation")]
    public async Task<IActionResult> SetVisibility(int id, ReviewModerationRequest request, CancellationToken ct)
    {
        if (id <= 0) return BadRequest("Review id must be positive.");
        if (User.GetUserId() is not int adminId || adminId <= 0) return Unauthorized();

        var hidden = request.IsHidden!.Value;
        var moderatedAt = clock.GetUtcNow().UtcDateTime;
        var review = db.Reviews.Where(row => row.ReviewId == id);
        var updated = await review.Where(row => row.IsHidden != hidden).ExecuteUpdateAsync(setters => setters
            .SetProperty(row => row.IsHidden, hidden)
            .SetProperty(row => row.ModeratedAt, moderatedAt)
            .SetProperty(row => row.ModeratedByUserId, adminId), ct);

        return updated > 0 || await review.AnyAsync(ct) ? NoContent() : NotFound();
    }
}
