using UniversityDecisionHub.Api.Features.Users;

namespace UniversityDecisionHub.Api.Features.Authentication.Dtos
{
    // What the API returns about an account. PasswordHash and NormalizedEmail are never included.
    public class UserProfileDto
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int DepartmentId { get; set; }
        public int StudyYear { get; set; }
        public bool IsAdministrator { get; set; }

        public static UserProfileDto From(UserAccount user) => new()
        {
            UserId = user.UserId,
            FullName = user.FullName,
            Email = user.Email,
            DepartmentId = user.DepartmentId,
            StudyYear = user.StudyYear,
            IsAdministrator = user.IsAdministrator
        };
    }
}
