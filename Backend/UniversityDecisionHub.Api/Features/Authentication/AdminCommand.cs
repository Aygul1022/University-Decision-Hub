using Microsoft.EntityFrameworkCore;
using UniversityDecisionHub.Api.Data;
using UniversityDecisionHub.Api.Features.Users;

namespace UniversityDecisionHub.Api.Features.Authentication
{
    // The only way to create an administrator:
    //   1. register normally through POST api/auth/register
    //   2. on the server machine run:  dotnet run -- make-admin someone@example.com
    // No password is passed or stored by this command, and nothing in the HTTP API can do this.
    public static class AdminCommand
    {
        public const string Name = "make-admin";

        public static async Task<int> RunAsync(IServiceProvider services, string email)
        {
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var normalizedEmail = UserAccount.NormalizeEmail(email);
            var user = await db.UserAccounts.FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail);
            if (user == null)
            {
                Console.Error.WriteLine($"No account is registered with the email '{email}'. Register it first.");
                return 1;
            }

            user.IsAdministrator = true;
            await db.SaveChangesAsync();
            Console.WriteLine($"'{user.Email}' is now an administrator. They must log in again to receive an administrator token.");
            return 0;
        }
    }
}
