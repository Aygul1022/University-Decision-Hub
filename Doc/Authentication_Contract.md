# Accounts, authentication and instructors — contract for the team

Owner: Ozan. This describes what the code in `Features/Users`, `Features/Authentication` and `Features/Instructors` does today, what the other modules can rely on, and which requirement IDs (Requirements v0.2) each part answers.

## 1. The `UserAccount` model (table `UserAccounts`)

One shared model, in `Features/Users/UserAccount.cs`; mapping in `Data/Configurations/UserAccountConfiguration.cs`.

| Column | PostgreSQL type | Null | Notes |
|---|---|---|---|
| `UserId` | `integer`, identity | no | Primary key |
| `FullName` | `character varying(150)` | no | Trimmed |
| `Email` | `character varying(254)` | no | As typed (trimmed); shown in the UI |
| `NormalizedEmail` | `character varying(254)` | no | `Email.Trim().ToUpperInvariant()`; **unique index** `IX_UserAccounts_NormalizedEmail` |
| `PasswordHash` | `text` | **yes** | Null until the person completes registration; then hash only — see section 2 |
| `DepartmentId` | `integer` | no | Positive; becomes a foreign key when the Department entity exists |
| `StudyYear` | `integer` | no | 1–6 (validated in the API) |
| `IsAdministrator` | `boolean` | no | Default `false`; never set from a request |
| `CreatedAtUtc` | `timestamp with time zone` | no | Set when the account is created (import or administrator) |

Compared with the fields discussed earlier (`UserId, FullName, Email, DepartmentId, StudyYear, IsAdministrator`), three columns are new: `NormalizedEmail`, `PasswordHash`, `CreatedAtUtc`. The unique index moves from `Email` to `NormalizedEmail`.

**Migration status: none has been created.** Aygül owns migration order, so the models are in the code but the migration for `UserAccounts` and `Instructors` (section 9) still has to be created/updated by her to match these tables. Until then:

- the auth and instructor endpoints fail against a real database (the tables do not exist);
- `Tests/UniversityDecisionHub.CourseChecks` reports two failures that are expected until the migration lands: "Only the two original course entities are implemented" and "Migration snapshot matches current model". Those two checks should be updated together with the migration.

## 2. Passwords

- Stored in exactly one column, `PasswordHash`, produced by ASP.NET Core Identity's `PasswordHasher<UserAccount>` (PBKDF2-HMAC-SHA512, 100 000 iterations, a random salt per user embedded in the value).
- The plain password is never stored, logged or returned. No API response contains `PasswordHash` or `NormalizedEmail`.
- Rules: 8–128 characters. Wrong password, unknown email and an account that has not completed registration all give the same `401` answer.
- Register and login share a limit of 10 attempts per minute per client address (`429` afterwards).
- Nobody — including administrators and whoever imports data — ever sets or knows another person's password. There are no default or shared passwords.

## 3. Email uniqueness without upper/lower-case differences

`NormalizedEmail` is computed by `UserAccount.NormalizeEmail(email)` on every register and login, and the database enforces uniqueness on it. `Ali@X.com` and `ali@x.com` are the same account. Any import or SQL script that inserts users must fill `NormalizedEmail` the same way: `UPPER(TRIM("Email"))`.

## 4. How accounts are created

Team decision (replaces the earlier registration-only proposal and the activation-code proposal): **accounts are defined in advance, and each person completes registration themselves.**

1. An account is created without a password, in one of two ways:
   - the private user list is imported into the local database with `import-users` (section 7), or
   - an administrator adds a single person with `POST /api/admin/users`.
2. The person opens the Register screen and enters full name, email, department, year of study, password and repeated password.
3. The backend looks the account up by email and compares name, department and year with the stored record. The name comparison ignores letter case, extra spaces and Turkish diacritics; department and year must be equal.
4. If everything matches and the account has no password yet, the password hash is stored and the person is logged in.
5. Afterwards the person can change their name, department and year on the My profile page (`PUT /api/auth/me`). Review eligibility must read these values from the database at submission time.

Rejected:

| Situation | Answer |
|---|---|
| Email not on the list, or name / department / year different | `404`, one message for all of these |
| Account already has a password | `409` |
| Missing or invalid field, password shorter than 8 | `400` |

There is no open registration: a person who is not on the list cannot create an account.

**Known limitation, accepted by the team:** this checks that the details match the list; it does not prove the person owns the email address. Someone who knows another student's name, email, department and year could register that account first. The first registration wins, the real owner then gets `409`, and an administrator has to step in. Email verification would close this gap and is a separate decision.

**Administrators:** a team member completes registration like everyone else, then on the machine that runs the backend:

```powershell
dotnet run --project Backend/UniversityDecisionHub.Api -- make-admin someone@example.com
```

The person then logs in again to get an administrator token. Nothing in the HTTP API can grant this, and neither the import file nor the add-user endpoint has an administrator field.

**Open points for the team:**

- **Graduates.** Two people on the current list answered "Mezun" instead of a year. `StudyYear` must be 1–6, so the import rejects those rows until the team decides how graduates are stored.
- **Department IDs.** The import file needs the numeric `DepartmentId` used in the database (Aygül's Department table), not the department name typed in the form.

## 5. Endpoints

| Request | Who | Body | Success | Errors |
|---|---|---|---|---|
| `POST /api/auth/register` | anyone on the list | `fullName, email, password, departmentId, studyYear` | `201` + `{ token, expiresAtUtc, user }` | `400`, `404` no match, `409` already registered, `429` |
| `POST /api/auth/login` | registered user | `email, password` | `200` + `{ token, expiresAtUtc, user }` | `401`, `429` |
| `GET /api/auth/me` | logged in | — | `200` + `user` | `401` |
| `PUT /api/auth/me` | logged in | `fullName, departmentId, studyYear` | `200` + `user` | `400`, `401` |
| `GET /api/admin/users` | administrator | — | `200` + list of `managedUser` | `401`, `403` |
| `GET /api/admin/users/{id}` | administrator | — | `200` + `managedUser` | `401`, `403`, `404` |
| `POST /api/admin/users` | administrator | `fullName, email, departmentId, studyYear` | `201` + `managedUser` | `400`, `401`, `403`, `409` email exists |
| `PUT /api/admin/users/{id}` | administrator | `fullName, email, departmentId, studyYear` | `200` + `managedUser` | `400`, `401`, `403`, `404`, `409` |

`user` is `{ userId, fullName, email, departmentId, studyYear, isAdministrator }`.
`managedUser` is the same plus `hasCompletedRegistration` (false while the person has not chosen a password).

No endpoint accepts or returns a password hash, and no endpoint lets one person set another person's password. There is no delete-user endpoint: reviews will reference their authors.

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
[Authorize(Roles = "Administrator")]                  // the same check, written with the role name

int? userId = User.GetUserId();                       // UserAccount.UserId of the caller
var profile = await _db.UserAccounts.FindAsync(userId); // department / study year for eligibility
```

The identity uses the standard claim types, so code that does not use the helpers works too:

| What | Claim | Value |
|---|---|---|
| Who is calling | `ClaimTypes.NameIdentifier` | The database `UserAccount.UserId`, as text |
| Administrator | `ClaimTypes.Role` | `"Administrator"` (absent for ordinary users) |

`User.FindFirstValue(ClaimTypes.NameIdentifier)` and `User.IsInRole("Administrator")` therefore both work. The value comes from the signed token, never from the request body, so a user cannot act under another user's id.

Read department and study year from the database (as above), not from the token — the token only carries the user id and the administrator role. An administrator flag changed with `make-admin` takes effect at that person's next login.

Existing course endpoints are unchanged and still unprotected; their owners add the attributes.

## 7. Local setup for each teammate

**Importing the private user list** (each teammate, into their own local database only):

```powershell
dotnet run --project Backend/UniversityDecisionHub.Api -- import-users "C:\path\outside\the\repository\users.csv"
```

- UTF-8 text, comma or semicolon separated, first line exactly `FullName,Email,DepartmentId,StudyYear`. A template with synthetic rows is in `Doc/users_import_template.csv`.
- No password, hash or administrator column. Rows with a missing value, an invalid email, a non-numeric department or a year outside 1–6 are rejected and reported by line number.
- Emails already in the database are skipped, so the command can be run again safely.
- **The real file must stay outside the repository.** `.gitignore` also blocks `private-data/` and `*.private.csv` as a safety net.

```powershell
cd Backend/UniversityDecisionHub.Api
dotnet user-secrets set "Jwt:Key" "<any 32+ random characters>"
```

Without it the backend still starts in Development, with a temporary key: everyone is logged out whenever the backend restarts. Outside Development the key is required. Never commit it.

Shared-file changes made for authentication: `Program.cs` (auth services, CORS for the HTML frontend, rate limiter, `UseAuthentication`, HTTPS redirect only outside Development), `ApplicationDbContext.cs` (`UserAccounts` set + configuration), the `.csproj` (`Microsoft.AspNetCore.Authentication.JwtBearer`).

## 8. Frontend

Every page loads the shared files in this order:

```html
<link rel="stylesheet" href="../../css/shared/global.css">
<link rel="stylesheet" href="../../css/shared/navbar.css">
...
<script src="../../js/shared/api.js"></script>
<script src="../../js/shared/navbar.js"></script>
```

- `css/shared/global.css` — the red / white / cream palette as CSS variables (UI02), base typography, `.page`, `.card`, `.button`, `.notice`. The university logo is not included; no asset has been chosen yet.
- `js/shared/api.js` — `apiFetch(path, { method, body })`, which adds the token automatically, and `Session` (`Session.user`, `Session.isLoggedIn`, `Session.clear()`). The backend address defaults to `http://localhost:5154`.
- `js/shared/navbar.js` — inserts the header by itself: Read Reviews, Write a Review, Instructors (UI03), and Log in / Register at the upper right, or the user's name and Log out when logged in. Pages write no navbar HTML.
- `Site.membersUrl("Pages/reviews/reviews.html")` gives the page itself to a logged-in user and the login page to a visitor, who is returned to that page after logging in (FR01 "More Reviews redirects to login"). Sami can use it for the landing-page link.

Pages:

| Page | Open as | Notes |
|---|---|---|
| `Pages/auth/login.html`, `register.html` | — | Register completes a pre-defined account; it asks for a department *number* until a Departments endpoint exists |
| `Pages/auth/profile.html` | — | Logged-in user edits name, department and year; reached from the name in the navbar |
| `Pages/courses/course-detail.html` | `?id=<CourseId>` | Reads Aygül's `GET /api/courses/{id}`; no second Course model |
| `Pages/reviews/instructors.html` | — | Name and department filters, empty state |
| `Pages/reviews/instructor-detail.html` | `?id=<InstructorId>` | Profile and course reviews are separate sections (UI05) |

Links to Sami's Read Reviews page use `reviews.html?courseCode=<code>` and `reviews.html?instructorId=<id>`. **These parameter names are a proposal — Sami, please confirm or tell me the names you use.** Until `reviews.html` and `review-form.html` exist, those navigation links lead to a missing page.

## 9. Instructors

Table `Instructors` (DR04), model in `Features/Instructors/Instructor.cs`:

| Column | PostgreSQL type | Null | Notes |
|---|---|---|---|
| `InstructorId` | `integer`, identity | no | Primary key |
| `FullName` | `character varying(150)` | no | Trimmed; not unique (two people can share a name) |
| `AcademicTitle` | `character varying(50)` | yes | Blank input is stored as null |
| `DepartmentId` | `integer` | no | Positive; indexed; becomes a foreign key when the Department entity exists |

An instructor is a catalog record, not a login account; there is no link to `UserAccounts`.

| Request | Who | Success | Errors |
|---|---|---|---|
| `GET /api/instructors?query=&departmentId=` | anyone | `200` + list sorted by name (`[]` when nothing matches) | `400` invalid filter |
| `GET /api/instructors/{id}` | anyone | `200` + instructor | `404` |
| `POST /api/instructors` | administrator | `201` + instructor | `400`, `401`, `403` |
| `PUT /api/instructors/{id}` | administrator | `200` + instructor | `400`, `401`, `403`, `404` |

Body for POST/PUT: `{ fullName, academicTitle, departmentId }`. Response: `{ instructorId, fullName, academicTitle, departmentId }`.

There is no DELETE: offerings and reviews will reference instructors and must keep their history (AT07). Aygül's `OfferingInstructor` can reference `Instructors.InstructorId` directly.

## 10. Requirement coverage and what is still open

| Requirement | Status |
|---|---|
| FR02 accounts | Accounts are defined in advance (import or administrator) and completed by their owner at Register; see section 4 |
| FR03 profiles | Department and study year are required at registration. Non-student accounts: unresolved in the requirements, not implemented |
| FR07 author identity | `User.GetUserId()` from the token; never from the request body |
| FR09 / FR11 administrator-only actions | `AuthPolicies.Administrator`; used by instructor writes. Course writes are still unprotected (Aygül's endpoints) |
| FR12 instructor pages | Navigation entry, list and detail done. **Associated courses are not shown yet** — they need Aygül's offerings; reviews are reached through Sami's page |
| FR13 profile changes | `PUT /api/auth/me` lets the user change name, department and year; eligibility is rechecked from the database at submission |
| DR01, DR04 | Sections 1 and 9 |
| UI01 / UI03 header | Shared navbar done; the landing page itself is Sami's |
| UI02 | Palette done; logo missing |

Choices made where the requirements leave a decision open — say so if the team wants otherwise:

- Basic instructor profiles are readable **without login** (FR12 leaves this open); reviews stay behind login.
- Static prerequisite links are **not** displayed on the course detail page (D09 open).
- Department names are shown as "Department 3" until a Departments endpoint exists.
- No page links to the course detail page yet: the course list page has no owner in the allocation.

## 11. Verification so far

The machine this was written on has only the .NET 9 SDK and no PostgreSQL, so the real `net10.0` solution was **not** built or run there. The same source files were compiled in a temporary .NET 9 project against SQLite and exercised over HTTP with synthetic data:

- 36 account checks: import (added / skipped / rejected rows, safe re-run), registration that matches or fails on each field, the same `404` for every kind of mismatch, no login before registration, a second registration cannot replace the password, profile update, user management as anonymous / student / administrator, an administrator-added user completing registration, hashes never returned and `NULL` for unregistered accounts.
- 23 instructor checks and 11 identity-claim checks from the earlier rounds (not re-run after this change, except the administrator instructor write and the claims probe, which passed).
- The Register and My profile pages were driven in a browser.

Still to do on a machine with .NET 10 and PostgreSQL: build the solution, apply Aygül's migration, and repeat the requests in `UniversityDecisionHub.Api.http`.
