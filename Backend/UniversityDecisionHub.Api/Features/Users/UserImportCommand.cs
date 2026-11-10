using System.ComponentModel.DataAnnotations;
using System.Text;
using Microsoft.EntityFrameworkCore;
using UniversityDecisionHub.Api.Data;
using UniversityDecisionHub.Api.Features.Users.Dtos;

namespace UniversityDecisionHub.Api.Features.Users
{
    // Loads the private list of pre-registered users into the LOCAL database:
    //   dotnet run -- import-users "C:\somewhere-outside-the-repository\users.csv"
    //
    // The file is UTF-8 text with this header (comma or semicolon separated):
    //   FullName,Email,DepartmentId,StudyYear
    //
    // It must never contain passwords or administrator flags, and it must never be committed.
    // Imported accounts have no password; each person completes registration themselves.
    // Running the command again is safe: emails that already exist are left untouched.
    public static class UserImportCommand
    {
        public const string Name = "import-users";

        private static readonly string[] Columns = ["FullName", "Email", "DepartmentId", "StudyYear"];

        public static async Task<int> RunAsync(IServiceProvider services, string path)
        {
            if (!File.Exists(path))
            {
                Console.Error.WriteLine($"File not found: {path}");
                return 1;
            }

            var lines = await File.ReadAllLinesAsync(path, Encoding.UTF8);
            if (lines.Length == 0)
            {
                Console.Error.WriteLine("The file is empty.");
                return 1;
            }

            var separator = lines[0].Contains(';') ? ';' : ',';
            var header = Split(lines[0], separator);
            if (!header.SequenceEqual(Columns, StringComparer.OrdinalIgnoreCase))
            {
                Console.Error.WriteLine("The first line must be exactly: " + string.Join(separator, Columns));
                return 1;
            }

            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var existing = (await db.UserAccounts.Select(u => u.NormalizedEmail).ToListAsync()).ToHashSet();

            int added = 0, alreadyPresent = 0, rejected = 0;
            for (var i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;

                // Problems are reported by line number only, so personal data is not echoed to the console.
                var problem = TryRead(Split(lines[i], separator), out var row);
                if (problem != null)
                {
                    Console.WriteLine($"Line {i + 1}: rejected - {problem}");
                    rejected++;
                    continue;
                }

                var normalizedEmail = UserAccount.NormalizeEmail(row.Email);
                if (!existing.Add(normalizedEmail))
                {
                    Console.WriteLine($"Line {i + 1}: skipped - this email is already present.");
                    alreadyPresent++;
                    continue;
                }

                db.UserAccounts.Add(new UserAccount
                {
                    FullName = row.FullName.Trim(),
                    Email = row.Email.Trim(),
                    NormalizedEmail = normalizedEmail,
                    PasswordHash = null,
                    DepartmentId = row.DepartmentId,
                    StudyYear = row.StudyYear,
                    IsAdministrator = false,
                    CreatedAtUtc = DateTime.UtcNow
                });
                added++;
            }

            await db.SaveChangesAsync();
            Console.WriteLine($"Import finished: {added} added, {alreadyPresent} already present, {rejected} rejected.");
            return rejected == 0 ? 0 : 2;
        }

        private static string[] Split(string line, char separator) =>
            line.Trim().TrimStart('\uFEFF').Split(separator).Select(value => value.Trim().Trim('"').Trim()).ToArray();

        // Returns null when the row is usable, otherwise the reason it is not.
        private static string? TryRead(string[] values, out ManagedUserSaveDto row)
        {
            row = new ManagedUserSaveDto();
            if (values.Length != Columns.Length)
                return $"expected {Columns.Length} values but found {values.Length}.";
            if (!int.TryParse(values[2], out var departmentId))
                return "DepartmentId is not a number.";
            if (!int.TryParse(values[3], out var studyYear))
                return "StudyYear is not a number.";

            row = new ManagedUserSaveDto { FullName = values[0], Email = values[1], DepartmentId = departmentId, StudyYear = studyYear };

            // The same rules as the administrator "add user" endpoint.
            var errors = new List<ValidationResult>();
            if (!Validator.TryValidateObject(row, new ValidationContext(row), errors, validateAllProperties: true))
                return string.Join(" ", errors.SelectMany(e => e.MemberNames).Distinct().Select(name => name + " is missing or invalid."));
            return null;
        }
    }
}
