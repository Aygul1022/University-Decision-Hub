using Microsoft.AspNetCore.Mvc;
using UniversityDecisionHub.Api.Features.Reviews.Dtos;
namespace UniversityDecisionHub.Api.Features.Reviews;
[ApiController, Route("api/reviews")]
public class ReviewSubmissionController(IReviewSubmissionService submissions) : ControllerBase
{
    // GET/PUT/DELETE routes are reserved for Sami's reading and administrator controllers.
    [HttpPost]
    public async Task<IActionResult> Post(ReviewCreateDto input, CancellationToken ct)
    {
        var result = await submissions.SubmitAsync(User, input, ct);
        if (result.Review is not null) return StatusCode(result.StatusCode, result.Review);
        return Problem(statusCode: result.StatusCode, title: result.Error);
    }
}
