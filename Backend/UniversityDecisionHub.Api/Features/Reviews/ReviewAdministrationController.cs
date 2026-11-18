using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniversityDecisionHub.Api.Data;
using UniversityDecisionHub.Api.Features.Authentication;

namespace UniversityDecisionHub.Api.Features.Reviews;

[ApiController]
[Route("api/reviews")]
[Authorize(Policy = AuthPolicies.Administrator)]
public sealed class ReviewAdministrationController(ApplicationDbContext db) : ControllerBase
{
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var deleted = await db.Reviews.Where(review => review.ReviewId == id).ExecuteDeleteAsync(ct);
        return deleted == 0
            ? Problem(statusCode: StatusCodes.Status404NotFound, type: "urn:udh:review-not-found", title: "Review not found.")
            : NoContent();
    }
}
