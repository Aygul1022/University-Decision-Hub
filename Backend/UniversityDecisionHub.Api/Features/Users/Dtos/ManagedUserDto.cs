namespace UniversityDecisionHub.Api.Features.Users.Dtos
{
    // What an administrator sees about an account. Never contains the password hash.
    public class ManagedUserDto
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int? DepartmentId { get; set; }
        public int? ProgrammeId { get; set; }
        public int? StudyYear { get; set; }
        public bool IsGraduate { get; set; }
        public bool IsAdministrator { get; set; }

        // False while the account exists but its owner has not chosen a password yet.
        public bool HasCompletedRegistration { get; set; }

        public static ManagedUserDto From(UserAccount user) => new()
        {
            UserId = user.UserId,
            FullName = user.FullName,
            Email = user.Email,
            DepartmentId = user.DepartmentId,
            ProgrammeId = user.ProgrammeId,
            StudyYear = user.StudyYear,
            IsGraduate = user.IsGraduate,
            IsAdministrator = user.IsAdministrator,
            HasCompletedRegistration = user.HasCompletedRegistration
        };
    }
}
