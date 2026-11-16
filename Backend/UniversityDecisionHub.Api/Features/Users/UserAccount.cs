using System.Text;

namespace UniversityDecisionHub.Api.Features.Users
{
    // The single shared account/profile model (table: UserAccounts).
    // Instructor records are separate and are NOT login accounts.
    //
    // Anyone can register an account with an email that has no account yet (open registration).
    // A record may also be added in advance (private local import, or by an administrator) WITHOUT a
    // password; it becomes an account when its owner registers with that email and chooses a password.
    public class UserAccount
    {
        public int UserId { get; set; }

        public string FullName { get; set; } = string.Empty;

        // Email as entered (trimmed). Shown in the UI.
        public string Email { get; set; } = string.Empty;

        // Trimmed + upper-cased copy of Email. The unique index lives on this column,
        // so "Ali@X.com" and "ali@x.com" can never become two accounts.
        public string NormalizedEmail { get; set; } = string.Empty;

        // Null until the person completes registration; an account without a hash cannot log in.
        // Otherwise the output of ASP.NET Core Identity's PasswordHasher (PBKDF2, per-user salt embedded).
        // The plain password is never stored and this value is never returned by the API.
        public string? PasswordHash { get; set; }

        // Student profile. Both are optional: an account without them can still log in,
        // but review submission requires a selected programme and study year.
        // DepartmentId is a foreign key to Departments.
        public int? DepartmentId { get; set; }
        public int? StudyYear { get; set; }
        // No programme is guessed from a legacy department. Profile integration follows separately.
        public int? ProgrammeId { get; set; }

        // Never set from a request body. Granted only with the `make-admin` command.
        public bool IsAdministrator { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        public bool HasCompletedRegistration => PasswordHash != null;

        public static string NormalizeEmail(string email) => email.Trim().ToUpperInvariant();

        // Used only to compare a typed name with the stored one during registration. Ignores letter case,
        // extra spaces and Turkish diacritics, so "yiğit  şahin" and "Yigit Sahin" are the same person.
        public static string NormalizeName(string name)
        {
            var result = new StringBuilder(name.Length);
            var pendingSpace = false;
            foreach (var original in name.Trim())
            {
                if (char.IsWhiteSpace(original))
                {
                    pendingSpace = true;
                    continue;
                }
                if (pendingSpace && result.Length > 0) result.Append(' ');
                pendingSpace = false;

                var folded = original switch
                {
                    'ı' or 'İ' or 'I' => 'i',
                    'ğ' or 'Ğ' => 'g',
                    'ü' or 'Ü' => 'u',
                    'ş' or 'Ş' => 's',
                    'ö' or 'Ö' => 'o',
                    'ç' or 'Ç' => 'c',
                    'â' or 'Â' => 'a',
                    'î' or 'Î' => 'i',
                    'û' or 'Û' => 'u',
                    _ => char.ToLowerInvariant(original)
                };
                result.Append(folded);
            }
            return result.ToString();
        }
    }
}
