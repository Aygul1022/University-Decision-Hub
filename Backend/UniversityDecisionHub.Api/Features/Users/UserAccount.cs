namespace UniversityDecisionHub.Api.Features.Users
{
    // The single shared account/profile model (table: UserAccounts).
    // Instructor records are separate and are NOT login accounts.
    public class UserAccount
    {
        public int UserId { get; set; }

        public string FullName { get; set; } = string.Empty;

        // Email as the user typed it (trimmed). Shown in the UI.
        public string Email { get; set; } = string.Empty;

        // Trimmed + upper-cased copy of Email. The unique index lives on this column,
        // so "Ali@X.com" and "ali@x.com" can never become two accounts.
        public string NormalizedEmail { get; set; } = string.Empty;

        // Output of ASP.NET Core Identity's PasswordHasher (PBKDF2, per-user salt embedded).
        // The plain password is never stored and this value is never returned by the API.
        public string PasswordHash { get; set; } = string.Empty;

        // Self-declared student profile. Becomes a foreign key once the Department entity exists.
        public int DepartmentId { get; set; }
        public int StudyYear { get; set; }

        // Never set from a request body. Granted only with the `make-admin` command.
        public bool IsAdministrator { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        public static string NormalizeEmail(string email) => email.Trim().ToUpperInvariant();
    }
}
