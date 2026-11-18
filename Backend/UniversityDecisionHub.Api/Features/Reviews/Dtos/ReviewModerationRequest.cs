using System.ComponentModel.DataAnnotations;

namespace UniversityDecisionHub.Api.Features.Reviews.Dtos;

public sealed class ReviewModerationRequest
{
    [Required]
    public bool? IsHidden { get; set; }
}
