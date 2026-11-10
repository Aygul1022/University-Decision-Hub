# Authentication and user accounts — contract for the team

Owner: Ozan. This describes what the code in `Features/Users` and `Features/Authentication` does today, and what the other modules can rely on.

## 1. The `UserAccount` model (table `UserAccounts`)

One shared model, in `Features/Users/UserAccount.cs`; mapping in `Data/Configurations/UserAccountConfiguration.cs`.

| Column | PostgreSQL type | Null | Notes |
|---|---|---|---|
| `UserId` | `integer`, identity | no | Primary key |
| `FullName` | `character varying(150)` | no | Trimmed |
| `Email` | `character varying(254)` | no | As typed (trimmed); shown in the UI |
| `NormalizedEmail` | `character varying(254)` | no | `Email.Trim().ToUpperInvariant()`; **unique index** `IX_UserAccounts_NormalizedEmail` |
| `PasswordHash` | `text` | no | Hash only — see section 2 |
| `DepartmentId` | `integer` | no | Positive; becomes a foreign key when the Department entity exists |
| `StudyYear` | `integer` | no | 1–6 (validated in the API) |
| `IsAdministrator` | `boolean` | no | Default `false`; never set from a request |
| `CreatedAtUtc` | `timestamp with time zone` | no | Set at registration |

Compared with the fields discussed earlier (`UserId, FullName, Email, DepartmentId, StudyYear, IsAdministrator`), three columns are new: `NormalizedEmail`, `PasswordHash`, `CreatedAtUtc`. The unique index moves from `Email` to `NormalizedEmail`.

**Migration status: none has been created.** Aygül owns migration order, so the model is in the code but the `UserAccounts` migration still has to be created/updated by her to match the table above. Until then:

- the auth endpoints fail against a real database (the table does not exist);
- `Tests/UniversityDecisionHub.CourseChecks` reports two failures that are expected until the migration lands: "Only the two original course entities are implemented" and "Migration snapshot matches current model". Those two checks should be updated together with the migration.

## 2. Passwords

- Stored in exactly one column, `PasswordHash`, produced by ASP.NET Core Identity's `PasswordHasher<UserAccount>` (PBKDF2-HMAC-SHA512, 100 000 iterations, a random salt per user embedded in the value).
- The plain password is never stored, logged or returned. No API response contains `PasswordHash` or `NormalizedEmail`.
- Rules: 8–128 characters. Wrong password and unknown email give the same `401` answer.
- Register and login share a limit of 10 attempts per minute per client address (`429` afterwards).
- Nobody — including administrators and whoever imports data — ever sets or knows another person's password. There are no default or shared passwords.

## 3. Email uniqueness without upper/lower-case differences

`NormalizedEmail` is computed by `UserAccount.NormalizeEmail(email)` on every register and login, and the database enforces uniqueness on it. `Ali@X.com` and `ali@x.com` are the same account. Any import or SQL script that inserts users must fill `NormalizedEmail` the same way: `UPPER(TRIM("Email"))`.

## 4. How accounts are created

- **Everyone registers through the Register screen** (`POST /api/auth/register`). No student account is created in advance.
- **Survey answers are not turned into accounts.** A survey row has no password, and this release has no email verification, so a pre-created profile could be claimed by anyone who knows the address. The real name/email/department/year data should not be loaded into `UserAccounts`.
- Because there are no pre-created accounts, there is no "set your password on first login" flow. If the team later wants pre-created profiles, that needs email verification first and is a separate piece of work.
- **Administrators:** register normally, then on the machine that runs the backend:

  ```powershell
  dotnet run --project Backend/UniversityDecisionHub.Api -- make-admin someone@example.com
  ```

  The person then logs in again to get an administrator token. Nothing in the HTTP API can grant this.

## 5. Endpoints

| Request | Body | Success | Errors |
|---|---|---|---|
| `POST /api/auth/register` | `fullName, email, password, departmentId, studyYear` | `201` + `{ token, expiresAtUtc, user }` | `400` validation, `409` email exists, `429` |
| `POST /api/auth/login` | `email, password` | `200` + `{ token, expiresAtUtc, user }` | `401`, `429` |
| `GET /api/auth/me` | — (token required) | `200` + `user` | `401` |

`user` is `{ userId, fullName, email, departmentId, studyYear, isAdministrator }`.

The token is a JWT valid for 120 minutes. Send it on every protected request:

```
Authorization: Bearer <token>
```

## 6. Using it in other modules (Aygül, Sami)

```csharp
using Microsoft.AspNetCore.Authorization;
using UniversityDecisionHub.Api.Features.Authentication;

[Authorize]                                           // any logged-in user
[Authorize(Policy = AuthPolicies.Administrator)]      // administrators only

int? userId = User.GetUserId();                       // UserAccount.UserId of the caller
var profile = await _db.UserAccounts.FindAsync(userId); // department / study year for eligibility
```

Read department and study year from the database (as above), not from the token — the token only carries the user id and the administrator role. An administrator flag changed with `make-admin` takes effect at that person's next login.

Existing course endpoints are unchanged and still unprotected; their owners add the attributes.

## 7. Local setup for each teammate

```powershell
cd Backend/UniversityDecisionHub.Api
dotnet user-secrets set "Jwt:Key" "<any 32+ random characters>"
```

Without it the backend still starts in Development, with a temporary key: everyone is logged out whenever the backend restarts. Outside Development the key is required. Never commit it.

Shared-file changes made for authentication: `Program.cs` (auth services, CORS for the HTML frontend, rate limiter, `UseAuthentication`, HTTPS redirect only outside Development), `ApplicationDbContext.cs` (`UserAccounts` set + configuration), the `.csproj` (`Microsoft.AspNetCore.Authentication.JwtBearer`).

## 8. Frontend

- `Frontend/Pages/auth/login.html`, `register.html`, `css/pages/auth.css`, `js/pages/auth.js`.
- `Frontend/js/shared/api.js` gives every page `apiFetch(path, { method, body })`, which adds the token automatically, and `Session` (`Session.user`, `Session.isLoggedIn`, `Session.clear()`).
- The Register page asks for a department *number* until a Departments endpoint exists.
- The backend address defaults to `http://localhost:5154`.

## 9. Verification so far

The machine this was written on has only the .NET 9 SDK and no PostgreSQL, so the real `net10.0` solution was **not** built or run there. The same source files were compiled in a temporary .NET 9 project against SQLite and exercised over HTTP: 19 checks passed (register, duplicate email in another case, validation, login, identical 401s, `/me`, tampered token, 403 for students on an admin-only endpoint, `make-admin`, rate limit, CORS preflight, hash-only storage, database-level unique index), and both pages were driven in a browser. Still to do on a machine with .NET 10 and PostgreSQL: build the solution, apply Aygül's migration, and repeat the requests in `UniversityDecisionHub.Api.http`.
