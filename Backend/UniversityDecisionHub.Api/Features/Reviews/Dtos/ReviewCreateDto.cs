using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
namespace UniversityDecisionHub.Api.Features.Reviews.Dtos;
// Author, timestamp and moderation fields cannot be supplied by the client.
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public class ReviewCreateDto
{
    [Range(1, int.MaxValue)] public int CourseId { get; set; }
    [Range(1, int.MaxValue)] public int OfferingId { get; set; }
    [Range(1, int.MaxValue)] public int InstructorId { get; set; }
    [Required] public string ReviewText { get; set; } = string.Empty;
}
