using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
namespace UniversityDecisionHub.Api.Features.Reviews.Dtos;
// Author, timestamp and moderation fields cannot be supplied by the client.
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public class ReviewCreateDto : IValidatableObject
{
    [Range(1, int.MaxValue)] public int CourseId { get; set; }
    [Required, RegularExpression(@"^[0-9]{4}-[0-9]{4}$")]
    public string AcademicYear { get; set; } = string.Empty;
    [Required, RegularExpression("^(Fall|Spring|Summer)$")]
    public string Semester { get; set; } = string.Empty;
    public bool AttendanceConfirmed { get; set; }
    [Range(1, int.MaxValue)] public int InstructorId { get; set; }
    [Required] public string ReviewText { get; set; } = string.Empty;
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (!AttendanceConfirmed)
            yield return new ValidationResult("Confirm that you took this course with this instructor.", [nameof(AttendanceConfirmed)]);
        var parts = AcademicYear?.Split('-');
        if (parts is { Length: 2 } && int.TryParse(parts[0], out var start) && int.TryParse(parts[1], out var end)
            && (start < 1 || end != start + 1))
            yield return new ValidationResult("Academic year must contain consecutive years.", [nameof(AcademicYear)]);
    }
}
