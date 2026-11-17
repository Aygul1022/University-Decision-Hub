using System.Text.Json.Serialization;
using UniversityDecisionHub.Api.Features.Users;

namespace UniversityDecisionHub.Api.Features.Authentication.Dtos
{
    // What the API returns about an account. PasswordHash and NormalizedEmail are never included.
    public class UserProfileDto
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int? DepartmentId { get; set; }
        public int? ProgrammeId { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? DepartmentName { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ProgrammeName { get; set; }
        public int? StudyYear { get; set; }
        public bool IsGraduate { get; set; }
        public bool IsAdministrator { get; set; }

        public static UserProfileDto From(
            UserAccount user, string? departmentName = null, string? programmeName = null) => new()
        {
            UserId = user.UserId,
            FullName = user.FullName,
            Email = user.Email,
            DepartmentId = user.DepartmentId,
            ProgrammeId = user.ProgrammeId,
            DepartmentName = departmentName,
            ProgrammeName = programmeName,
            StudyYear = user.StudyYear,
            IsGraduate = user.IsGraduate,
            IsAdministrator = user.IsAdministrator
        };
    }
}
