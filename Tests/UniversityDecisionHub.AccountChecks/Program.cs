using UniversityDecisionHub.Api.Features.Programmes;
using UniversityDecisionHub.Api.Features.CatalogImport;
using System.ComponentModel.DataAnnotations;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using UniversityDecisionHub.Api.Data;
using UniversityDecisionHub.Api.Features.Authentication;
using UniversityDecisionHub.Api.Features.Authentication.Dtos;
using UniversityDecisionHub.Api.Features.Courses;
using UniversityDecisionHub.Api.Features.Departments;
using UniversityDecisionHub.Api.Features.Faculties;
using UniversityDecisionHub.Api.Features.Offerings;
using UniversityDecisionHub.Api.Features.Reviews;
using UniversityDecisionHub.Api.Features.Users;
using UniversityDecisionHub.Api.Features.Users.Dtos;

// Account, administrator-boundary, instructor and detail-page checks (Ozan's module), plus the
// hand-over to review submission: the token identifies the author and an incomplete profile is refused.
//
//   dotnet run --project Tests/UniversityDecisionHub.AccountChecks
//
// The real controllers, authentication setup and rate limiter are hosted on a random local port
// and called over HTTP. The database is in-memory SQLite, created from the EF model, so neither
// PostgreSQL nor a migration is needed. Every name, email and password below is synthetic.
//
// Register and login share a limit of 10 requests per minute, so exactly 10 such requests are
// made (marked "auth request n/10") and the 11th is expected to be refused.

var passed = 0;
var failures = new List<string>();
void Check(bool condition, string name)
{
    if (condition) { passed++; Console.WriteLine("PASS: " + name); }
    else { failures.Add(name); Console.WriteLine("FAIL: " + name); }
}

// ---------------------------------------------------------------- host
using var connection = new SqliteConnection("Data Source=:memory:");
connection.Open(); // the in-memory database lives as long as this connection stays open

var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Production" });
builder.WebHost.UseUrls("http://127.0.0.1:0");
builder.Logging.ClearProviders();
builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
{
    ["Jwt:Key"] = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48))
});
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(connection));
var apiAssembly = typeof(AuthController).Assembly;
builder.Services.AddControllers().ConfigureApplicationPartManager(manager =>
{
    if (!manager.ApplicationParts.OfType<AssemblyPart>().Any(part => part.Assembly == apiAssembly))
        manager.ApplicationParts.Add(new AssemblyPart(apiAssembly));
});
builder.Services.AddUdhAuthentication(builder.Configuration, builder.Environment);
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<IReviewSubmissionService, ReviewSubmissionService>();

// Same order as the API's Program.cs.
await using var app = builder.Build();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
await app.StartAsync();

using var http = new HttpClient { BaseAddress = new Uri(app.Urls.First()) };

async Task<Reply> Send(HttpMethod method, string path, object? body = null, string? token = null)
{
    using var request = new HttpRequestMessage(method, path);
    if (body != null) request.Content = JsonContent.Create(body);
    if (token != null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
    using var response = await http.SendAsync(request);
    return new Reply((int)response.StatusCode, await response.Content.ReadAsStringAsync());
}
Task<Reply> Get(string path, string? token = null) => Send(HttpMethod.Get, path, null, token);
Task<Reply> Post(string path, object? body, string? token = null) => Send(HttpMethod.Post, path, body, token);
Task<Reply> Put(string path, object? body, string? token = null) => Send(HttpMethod.Put, path, body, token);

async Task<T> Db<T>(Func<ApplicationDbContext, Task<T>> work)
{
    using var scope = app.Services.CreateScope();
    return await work(scope.ServiceProvider.GetRequiredService<ApplicationDbContext>());
}

// ---------------------------------------------------------------- test data
const string PasswordOne = "synthetic-pass-one";
const string PasswordTwo = "synthetic-pass-two";
const string EmailOne = "yigit.sahin@example.com";
const string EmailTwo = "student.two@example.com";
const string EmailNever = "never.registered@example.com";
const string EmailStaff = "staff.member@example.com";
const string PasswordStaff = "synthetic-pass-staff";

UserAccount Listed(string fullName, string email, int? departmentId, int? studyYear) => new()
{
    FullName = fullName, Email = email, NormalizedEmail = UserAccount.NormalizeEmail(email),
    DepartmentId = departmentId, StudyYear = studyYear, CreatedAtUtc = DateTime.UtcNow
};

var courseId = await Db(async db =>
{
    await db.Database.EnsureCreatedAsync();
    // Users, instructors and courses reference Departments, so five synthetic departments come first.
    db.Faculties.Add(new Faculty
    {
        FacultyName = "Synthetic Faculty",
        Departments = Enumerable.Range(1, 5).Select(n => new Department
        {
            DepartmentId = n, DepartmentCode = "DEP" + n, DepartmentName = "Synthetic Department " + n
        }).ToList()
    });
    await db.SaveChangesAsync();
    // The only record added in advance. It has a programme, so the checks below can prove that a refused
    // registration leaves the programme and every other stored field untouched.
    var listedProgramme = new Programme { ProgrammeKey = "TEST-P0", ProgrammeName = "Listed Programme", DepartmentId = 1 };
    db.Programmes.Add(listedProgramme);
    await db.SaveChangesAsync();
    var never = Listed("Never Registered", EmailNever, 1, 1);
    never.ProgrammeId = listedProgramme.ProgrammeId;
    db.UserAccounts.Add(never);
    var course = new Course
    {
        CourseCode = "CS101", CourseName = "Introduction to Computing", CourseDescription = "Synthetic course.",
        DepartmentId = 1, RecommendedStudyYear = 1, CourseCredit = 3, Ects = 6, IsActive = true, IsElective = false
    };
    db.Courses.Add(course);
    await db.SaveChangesAsync();
    return course.CourseId;
});

// ================================================================ registration and login
// auth request 1/10 — open registration with surrounding spaces in the name
var register = await Post("/api/auth/register", new
{
    fullName = "  Yiğit Şahin ", email = EmailOne, departmentId = 1, studyYear = 3, password = PasswordOne
});
Check(register.Status == 201, "Open registration: a new email creates an account (201)");
var tokenOne = register.Text("token") ?? "";
Check(tokenOne.Length > 0, "Registration returns a token");
Check(register.Json.TryGetProperty("user", out var registeredUser)
      && registeredUser.GetProperty("fullName").GetString() == "Yiğit Şahin"
      && registeredUser.GetProperty("email").GetString() == EmailOne,
    "Registration answers with the trimmed name and email");
Check(register.HasNoPrivateFields(PasswordOne), "Registration response has no password, hash or normalized email");
Check(await Db(db => db.UserAccounts.CountAsync(u => u.NormalizedEmail == UserAccount.NormalizeEmail(EmailOne))) == 1,
    "Open registration creates exactly one account for the email");

// auth request 2/10 — a record added in advance without a password is not handed over (email ownership is
// unverified). The request sends other details and no programmeId; the stored row must stay as it was.
var claim = await Post("/api/auth/register", new
{
    fullName = "Someone Claiming", email = "Never.Registered@Example.com", departmentId = 2, studyYear = 4, password = "another-password"
});
Check(claim.Status == 403 && claim.Body.Contains("added in advance"),
    "A record added in advance without a password cannot be registered (403)");
Check(claim.Text("token") == null, "The refused registration returns no token");
var neverAfterClaim = await Db(db => db.UserAccounts.AsNoTracking().SingleAsync(u => u.NormalizedEmail == UserAccount.NormalizeEmail(EmailNever)));
var listedProgrammeId = await Db(db => db.Programmes.Where(p => p.ProgrammeKey == "TEST-P0").Select(p => p.ProgrammeId).SingleAsync());
Check(neverAfterClaim.PasswordHash == null && neverAfterClaim.FullName == "Never Registered" && neverAfterClaim.Email == EmailNever
      && neverAfterClaim.DepartmentId == 1 && neverAfterClaim.StudyYear == 1 && !neverAfterClaim.IsGraduate,
    "The refused registration leaves the pre-added name, email, department and year unchanged, with no password");
Check(neverAfterClaim.ProgrammeId == listedProgrammeId,
    "Regression: a registration without programmeId does not clear a programme that was assigned in advance");

// auth request 3/10 — open registration: an email that has no account and is on no list
const string EmailNew = "new.student@example.com";
const string PasswordNew = "synthetic-pass-new";
var registerNew = await Post("/api/auth/register", new
{
    fullName = "New Student", email = "New.Student@Example.com", departmentId = 2, studyYear = (int?)null, isGraduate = true,
    password = PasswordNew
});
Check(registerNew.Status == 201 && !string.IsNullOrEmpty(registerNew.Text("token")),
    "Open registration: an email without an account creates one and logs in (201)");
Check(registerNew.HasNoPrivateFields(PasswordNew), "Open registration response has no password, hash or normalized email");
Check(await Db(db => db.UserAccounts.AnyAsync(u => u.NormalizedEmail == UserAccount.NormalizeEmail(EmailNew)
        && u.FullName == "New Student" && u.DepartmentId == 2
        && u.PasswordHash != null && !u.IsAdministrator)),
    "Open registration stores the typed details with a password hash and no administrator access");
Check(registerNew.Json.GetProperty("user").GetProperty("isGraduate").GetBoolean()
      && registerNew.Json.GetProperty("user").GetProperty("studyYear").ValueKind == JsonValueKind.Null
      && await Db(db => db.UserAccounts.AnyAsync(u => u.NormalizedEmail == UserAccount.NormalizeEmail(EmailNew)
          && u.IsGraduate && u.StudyYear == null)),
    "Registering as a graduate stores IsGraduate with no study year and returns it");
Check(registeredUser.GetProperty("isGraduate").GetBoolean() == false, "A registration with a study year is not a graduate");

// auth request 4/10 — the same email again, in another letter case
var duplicateNew = await Post("/api/auth/register", new
{
    fullName = "Someone Else", email = EmailNew.ToUpperInvariant(), departmentId = 2, studyYear = 4, password = PasswordTwo
});
Check(duplicateNew.Status == 409 && duplicateNew.Body.Contains("already exists") && duplicateNew.Body != claim.Body,
    "An email that already has an account cannot register again, whatever its letter case (409)");
Check(await Db(db => db.UserAccounts.CountAsync(u => u.NormalizedEmail == UserAccount.NormalizeEmail(EmailNew))) == 1
      && await Db(db => db.UserAccounts.AnyAsync(u => u.NormalizedEmail == UserAccount.NormalizeEmail(EmailNew) && u.FullName == "New Student")),
    "The rejected registration leaves the existing account unchanged");

// auth request 5/10 — the body asks for administrator access
var registerTwo = await Post("/api/auth/register", new
{
    fullName = "Student Two", email = EmailTwo, departmentId = 2, studyYear = 4, password = PasswordTwo,
    isAdministrator = true, role = "Administrator"
});
var staleTokenTwo = registerTwo.Text("token") ?? "";
Check(registerTwo.Status == 201 && registerTwo.Json.GetProperty("user").GetProperty("isAdministrator").GetBoolean() == false,
    "Registration ignores an isAdministrator value in the request");
Check(!await Db(db => db.UserAccounts.AnyAsync(u => u.IsAdministrator)), "No administrator exists after public registrations");

// auth request 6/10 — an account without department and year registers and is logged in
var registerStaff = await Post("/api/auth/register", new
{
    fullName = "Staff Member", email = EmailStaff, departmentId = (int?)null, studyYear = (int?)null, password = PasswordStaff
});
var tokenStaff = registerStaff.Text("token") ?? "";
Check(registerStaff.Status == 201 && tokenStaff.Length > 0, "An account without department and year can register and log in");

// Checked on the request type itself, to keep the 10 register/login requests for the cases above.
bool Valid(object dto) => Validator.TryValidateObject(dto, new ValidationContext(dto), [], true);
RegisterRequest Request(string password, int? studyYear = 1) => new()
{
    FullName = "Never Registered", Email = EmailNever, DepartmentId = 1, StudyYear = studyYear, Password = password
};
Check(Valid(Request("12345678")) && !Valid(Request("1234567")), "A 7-character password is rejected; 8 characters are accepted");
Check(!Valid(Request("12345678", studyYear: 0)) && !Valid(Request("12345678", studyYear: 7)) && Valid(Request("12345678", studyYear: null)),
    "Study year may be empty, but 0 and 7 are rejected");

// auth request 7/10
var login = await Post("/api/auth/login", new { email = "YIGIT.SAHIN@EXAMPLE.COM", password = PasswordOne });
Check(login.Status == 200 && !string.IsNullOrEmpty(login.Text("token")), "Login succeeds, whatever the letter case of the email");
Check(login.HasNoPrivateFields(PasswordOne), "Login response has no password, hash or normalized email");

// auth request 8/10
var wrongPassword = await Post("/api/auth/login", new { email = EmailOne, password = PasswordOne + "x" });
Check(wrongPassword.Status == 401, "Login with a wrong password is rejected (401)");

// auth request 9/10
var notCompleted = await Post("/api/auth/login", new { email = EmailNever, password = "1234567" });
Check(notCompleted.Status == 401 && notCompleted.Body == wrongPassword.Body,
    "An account that never completed registration cannot log in, with the same answer as a wrong password");

// ---- administrator access comes only from the make-admin command
var originalError = Console.Error;
Console.SetError(TextWriter.Null);
var unknownAdmin = await AdminCommand.RunAsync(app.Services, "nobody@example.com");
Console.SetError(originalError);
var originalOut = Console.Out;
Console.SetOut(TextWriter.Null);
var madeAdmin = await AdminCommand.RunAsync(app.Services, "  STUDENT.TWO@example.com ");
Console.SetOut(originalOut);
Check(unknownAdmin == 1, "make-admin fails for an email that has no account");
Check(madeAdmin == 0, "make-admin promotes an existing account");

// auth request 10/10
var adminLogin = await Post("/api/auth/login", new { email = EmailTwo, password = PasswordTwo });
var adminToken = adminLogin.Text("token") ?? "";
Check(adminLogin.Status == 200 && adminLogin.Json.GetProperty("user").GetProperty("isAdministrator").GetBoolean(),
    "After make-admin, a new login is an administrator login");

// 11th request inside the same minute
var limited = await Post("/api/auth/login", new { email = EmailOne, password = PasswordOne });
Check(limited.Status == 429, "The 11th register/login request within a minute is refused (429)");

// ================================================================ stored passwords
var stored = await Db(db => db.UserAccounts.AsNoTracking().ToDictionaryAsync(u => u.Email));
var hashOne = stored[EmailOne].PasswordHash;
Check(hashOne != null && hashOne != PasswordOne && !hashOne.Contains(PasswordOne), "The database holds a hash, not the password");
Check(hashOne != null && app.Services.GetRequiredService<IPasswordHasher<UserAccount>>()
        .VerifyHashedPassword(stored[EmailOne], hashOne, PasswordOne) != PasswordVerificationResult.Failed,
    "The stored hash verifies the first password, so the rejected second registration did not replace it");
Check(stored[EmailNever].PasswordHash == null, "An account that has not registered has no password");

// ================================================================ current user
Check((await Get("/api/auth/me")).Status == 401, "GET /api/auth/me without a token is rejected (401)");

var me = await Get("/api/auth/me", tokenOne);
Check(me.Status == 200 && me.Json.GetProperty("departmentId").GetInt32() == 1 && me.Json.GetProperty("studyYear").GetInt32() == 3
      && me.Json.GetProperty("userId").GetInt32() == stored[EmailOne].UserId,
    "GET /api/auth/me returns the caller's own id, department and study year");
Check(
    me.PropertyNames().SetEquals([
        "userId", "fullName", "email",
        "departmentId", "programmeId", "departmentName",
        "studyYear", "isGraduate", "isAdministrator"
    ])
    && me.Text("departmentName") == "Synthetic Department 1",
    "Profile exposes the expected public fields and matching department name"
);

var middle = tokenOne.Length - 10;
var tampered = tokenOne[..middle] + (tokenOne[middle] == 'A' ? 'B' : 'A') + tokenOne[(middle + 1)..];
Check((await Get("/api/auth/me", tampered)).Status == 401, "A token with a changed signature is rejected (401)");

var foreignKey = new JwtOptions { Key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48)) };
var forged = new JwtTokenService(foreignKey).CreateToken(new UserAccount { UserId = stored[EmailOne].UserId, IsAdministrator = true }).Token;
Check((await Get("/api/admin/users", forged)).Status == 401, "An administrator token signed with another key is rejected (401)");

var update = await Put("/api/auth/me", new
{
    fullName = "  Yiğit Şahin Kaya ", departmentId = 2, studyYear = 4, email = "changed@example.com", isAdministrator = true
}, tokenOne);
Check(update.Status == 200 && update.Text("fullName") == "Yiğit Şahin Kaya" && update.Json.GetProperty("departmentId").GetInt32() == 2
      && update.Json.GetProperty("studyYear").GetInt32() == 4,
    "PUT /api/auth/me updates name, department and study year");
Check(update.Text("email") == EmailOne && !update.Json.GetProperty("isAdministrator").GetBoolean(),
    "PUT /api/auth/me cannot change the email or grant administrator access");
var meAfter = await Get("/api/auth/me", tokenOne);
Check(meAfter.Json.GetProperty("departmentId").GetInt32() == 2 && meAfter.Json.GetProperty("studyYear").GetInt32() == 4,
    "The profile is read from the database, so an old token sees the new department and year");
Check((await Put("/api/auth/me", new { fullName = "Yiğit Şahin", departmentId = 1, studyYear = 9 }, tokenOne)).Status == 400,
    "PUT /api/auth/me rejects study year 9 (400)");
Check((await Put("/api/auth/me", new { fullName = "Yiğit Şahin Kaya", departmentId = 999, studyYear = 4 }, tokenOne)).Status == 400,
    "PUT /api/auth/me rejects a department that does not exist (400)");
var graduateWithYear = await Put("/api/auth/me", new { fullName = "Staff Member", studyYear = 4, isGraduate = true }, tokenStaff);
Check(graduateWithYear.Status == 400 && graduateWithYear.Body.Contains("graduate has no study year"),
    "Profile: Graduate together with a study year is rejected (400)");
var becomeGraduate = await Put("/api/auth/me", new { fullName = "Staff Member", isGraduate = true }, tokenStaff);
Check(becomeGraduate.Status == 200 && becomeGraduate.Json.GetProperty("isGraduate").GetBoolean()
      && becomeGraduate.Json.GetProperty("studyYear").ValueKind == JsonValueKind.Null
      && (await Get("/api/auth/me", tokenStaff)).Json.GetProperty("isGraduate").GetBoolean(),
    "Profile: a user can choose Graduate and it is saved and returned");
var noLongerGraduate = await Put("/api/auth/me", new { fullName = "Staff Member" }, tokenStaff);
Check(noLongerGraduate.Status == 200 && !noLongerGraduate.Json.GetProperty("isGraduate").GetBoolean()
      && !await Db(db => db.UserAccounts.AnyAsync(u => u.NormalizedEmail == UserAccount.NormalizeEmail(EmailStaff) && u.IsGraduate)),
    "Profile: leaving Graduate out clears it, like the other profile fields");
var constraintHeld = false;
try
{
    await Db(async db =>
    {
        db.UserAccounts.Add(new UserAccount
        {
            FullName = "Constraint Probe", Email = "constraint.probe@example.com",
            NormalizedEmail = UserAccount.NormalizeEmail("constraint.probe@example.com"),
            StudyYear = 2, IsGraduate = true, CreatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
        return 0;
    });
}
catch (DbUpdateException)
{
    constraintHeld = true;
}
Check(constraintHeld, "Database: a graduate with a study year is refused by the check constraint");

var meStaff = await Get("/api/auth/me", tokenStaff);
Check(meStaff.Status == 200 && meStaff.Json.GetProperty("departmentId").ValueKind == JsonValueKind.Null
      && meStaff.Json.GetProperty("studyYear").ValueKind == JsonValueKind.Null,
    "The profile of an account without department and year shows both as null");
Check((await Put("/api/auth/me", new { fullName = "Yiğit Şahin", departmentId = 1, studyYear = 3 })).Status == 401,
    "PUT /api/auth/me without a token is rejected (401)");

// ================================================================ administrator boundary: users
Check((await Get("/api/admin/users")).Status == 401, "User list: anonymous is rejected (401)");
Check((await Get("/api/admin/users", tokenOne)).Status == 403, "User list: a student is rejected (403)");
Check((await Get("/api/admin/users", staleTokenTwo)).Status == 403,
    "User list: a token issued before make-admin has no administrator access (403)");

var users = await Get("/api/admin/users", adminToken);
Check(users.Status == 200 && users.Json.GetArrayLength() == 5, "User list: an administrator sees all five accounts");
Check(users.HasNoPrivateFields(PasswordOne), "User list has no password, hash or normalized email");
Check(users.Json.EnumerateArray().Single(u => u.GetProperty("email").GetString() == EmailNever)
        .GetProperty("hasCompletedRegistration").GetBoolean() == false,
    "User list shows who has not completed registration yet");
// Register and the administrator list read the same UserAccounts table.
Check(users.Json.EnumerateArray().Count(u => string.Equals(u.GetProperty("email").GetString(), EmailNew, StringComparison.OrdinalIgnoreCase)
        && u.GetProperty("fullName").GetString() == "New Student"
        && u.GetProperty("hasCompletedRegistration").GetBoolean()) == 1,
    "User list: an account created by open registration appears once, as registered");
Check(users.Json.EnumerateArray().Single(u => string.Equals(u.GetProperty("email").GetString(), EmailNew, StringComparison.OrdinalIgnoreCase))
        .GetProperty("isGraduate").GetBoolean()
      && users.Json.EnumerateArray().Count(u => u.GetProperty("isGraduate").GetBoolean()) == 1,
    "User list shows who is a graduate");
Check(users.Json.EnumerateArray().Count(u => u.GetProperty("email").GetString() == EmailNever) == 1
      && users.Json.EnumerateArray().Single(u => u.GetProperty("email").GetString() == EmailNever)
          .GetProperty("programmeId").GetInt32() == listedProgrammeId,
    "User list: the record added in advance is still one row with its programme after the refused registration");

var newUser = new { fullName = " New Person ", email = "new.person@example.com", departmentId = 1, studyYear = 2, isAdministrator = true, password = "ignored-value" };
Check((await Post("/api/admin/users", newUser)).Status == 401, "Add user: anonymous is rejected (401)");
Check((await Post("/api/admin/users", newUser, tokenOne)).Status == 403, "Add user: a student is rejected (403)");
Check((await Post("/api/admin/users", new { fullName = "Graduate With Year", email = "graduate.year@example.com", studyYear = 3, isGraduate = true }, adminToken)).Status == 400,
    "Add user: Graduate together with a study year is rejected (400)");
var added = await Post("/api/admin/users", newUser, adminToken);
Check(added.Status == 201 && added.Text("fullName") == "New Person" && !added.Json.GetProperty("isAdministrator").GetBoolean()
      && !added.Json.GetProperty("hasCompletedRegistration").GetBoolean(),
    "Add user: an administrator adds a person, who is not an administrator and has no password");
var newUserId = added.Status == 201 ? added.Json.GetProperty("userId").GetInt32() : 0;
Check(await Db(db => db.UserAccounts.AnyAsync(u => u.UserId == newUserId && u.PasswordHash == null && !u.IsAdministrator)),
    "Add user: password and isAdministrator in the request are ignored in the database too");

var duplicate = await Post("/api/admin/users", new { fullName = "Other Name", email = "NEW.PERSON@EXAMPLE.COM", departmentId = 3, studyYear = 1 }, adminToken);
Check(duplicate.Status == 409, "Add user: the same email in another letter case is a duplicate (409)");
Check((await Post("/api/admin/users", new { fullName = "Bad Email", email = "not-an-email", departmentId = 1, studyYear = 1 }, adminToken)).Status == 400,
    "Add user: an invalid email is rejected (400)");

Check((await Post("/api/admin/users", new { fullName = "Wrong Department", email = "wrong.department@example.com", departmentId = 999, studyYear = 1 }, adminToken)).Status == 400,
    "Add user: a department that does not exist is rejected (400)");

var takeEmail = await Put("/api/admin/users/" + newUserId, new { fullName = "New Person", email = EmailOne, departmentId = 1, studyYear = 2 }, adminToken);
Check(takeEmail.Status == 409, "Edit user: changing the email to one that is in use is rejected (409)");
Check((await Put("/api/admin/users/999999", new { fullName = "No One", email = "no.one@example.com", departmentId = 1, studyYear = 1 }, adminToken)).Status == 404,
    "Edit user: an unknown id is not found (404)");
Check((await Put("/api/admin/users/" + newUserId, new { fullName = "New Person", email = "new.person@example.com", departmentId = 1, studyYear = 2 }, tokenOne)).Status == 403,
    "Edit user: a student is rejected (403)");

var uniqueIndexHolds = await Db(async db =>
{
    db.UserAccounts.Add(Listed("Copy", " Yigit.Sahin@EXAMPLE.com", 1, 1));
    try { await db.SaveChangesAsync(); return false; }
    catch (DbUpdateException) { return true; }
});
Check(uniqueIndexHolds, "The database itself refuses a second account with the same email");

// ================================================================ import command
var csvPath = Path.Combine(Path.GetTempPath(), "udh-account-checks-" + Guid.NewGuid().ToString("N") + ".csv");
await File.WriteAllLinesAsync(csvPath,
[
    "FullName,Email,DepartmentId,StudyYear",
    "Imported Person,imported.person@example.com,2,1",
    "Yiğit Şahin,YIGIT.SAHIN@example.com,1,3",
    "Broken Year,broken.year@example.com,1,x",
    "Year Nine,year.nine@example.com,1,9",
    "No Profile,no.profile@example.com,,",
    "Wrong Department,wrong.department@example.com,999,1"
]);
var output = new StringWriter();
Console.SetOut(output);
int firstImport, secondImport;
try
{
    firstImport = await UserImportCommand.RunAsync(app.Services, csvPath);
    secondImport = await UserImportCommand.RunAsync(app.Services, csvPath);
}
finally
{
    Console.SetOut(originalOut);
    File.Delete(csvPath);
}
var importText = output.ToString();
Check(firstImport == 2 && importText.Contains("2 added, 1 already present, 3 rejected"),
    "Import adds the two new rows, skips the existing email and rejects the three invalid rows");
Check(secondImport == 2 && importText.Contains("0 added, 3 already present, 3 rejected"), "Running the import again adds nothing");
Check(await Db(db => db.UserAccounts.AnyAsync(u => u.Email == "no.profile@example.com" && u.DepartmentId == null && u.StudyYear == null)),
    "Import accepts a row with empty department and year");
Check(!await Db(db => db.UserAccounts.AnyAsync(u => u.Email == "wrong.department@example.com")),
    "Import rejects a department that does not exist");
Check(!importText.Contains("@"), "Import output reports line numbers only, no email addresses");
Check(await Db(db => db.UserAccounts.AnyAsync(u => u.Email == "imported.person@example.com" && u.PasswordHash == null && !u.IsAdministrator)),
    "An imported account has no password and is not an administrator");
Check(await Db(db => db.UserAccounts.CountAsync()) == 8, "Eight accounts exist: four listed, one registered, one added, two imported");

// ================================================================ model-level guarantees
Check(UserAccount.NormalizeName("  yiğit   ŞAHİN ") == UserAccount.NormalizeName("Yigit Sahin"),
    "Name comparison ignores case, spacing and Turkish diacritics");
Check(new[] { typeof(RegisterRequest), typeof(ProfileUpdateRequest), typeof(ManagedUserSaveDto) }
        .All(type => type.GetProperty("IsAdministrator") == null),
    "No request body type has an IsAdministrator property");
Check(new[] { typeof(UserProfileDto), typeof(ManagedUserDto), typeof(AuthResponse) }
        .All(type => type.GetProperties().All(p => !p.Name.Contains("Password") && !p.Name.Contains("Normalized"))),
    "No response type has a password or normalized-email property");

// ================================================================ instructors
var empty = await Get("/api/instructors");
Check(empty.Status == 200 && empty.Json.ValueKind == JsonValueKind.Array && empty.Json.GetArrayLength() == 0,
    "Instructor list is readable without login and is an empty array at first");

var ada = new { fullName = "  Ada Yılmaz ", academicTitle = "   ", departmentId = 3 };
Check((await Post("/api/instructors", ada)).Status == 401, "Add instructor: anonymous is rejected (401)");
Check((await Post("/api/instructors", ada, tokenOne)).Status == 403, "Add instructor: a student is rejected (403)");
var createdAda = await Post("/api/instructors", ada, adminToken);
Check(createdAda.Status == 201 && createdAda.Text("fullName") == "Ada Yılmaz", "Add instructor: an administrator creates one; the name is trimmed");
Check(createdAda.Status == 201 && createdAda.Json.GetProperty("academicTitle").ValueKind == JsonValueKind.Null,
    "Add instructor: a blank title is stored as null");
var adaId = createdAda.Status == 201 ? createdAda.Json.GetProperty("instructorId").GetInt32() : 0;
var createdBurak = await Post("/api/instructors", new { fullName = "Burak Demir", academicTitle = "Prof. Dr.", departmentId = 4 }, adminToken);
Check(createdBurak.Status == 201, "Add instructor: a second record with a title is created");

var invalidInstructors = new (object Body, string Why)[]
{
    (new { fullName = "", departmentId = 3 }, "an empty name"),
    (new { fullName = "A", departmentId = 3 }, "a one-letter name"),
    (new { fullName = new string('n', 151), departmentId = 3 }, "a 151-character name"),
    (new { fullName = "Valid Name", academicTitle = new string('t', 51), departmentId = 3 }, "a 51-character title"),
    (new { fullName = "Valid Name", departmentId = 0 }, "department 0"),
    (new { fullName = "Valid Name" }, "a missing department"),
    (new { fullName = "Valid Name", departmentId = 999 }, "a department that does not exist"),
};
foreach (var (body, why) in invalidInstructors)
    Check((await Post("/api/instructors", body, adminToken)).Status == 400, "Add instructor: " + why + " is rejected (400)");
Check(await Db(db => db.Instructors.CountAsync()) == 2, "Rejected instructor requests stored nothing");

var all = await Get("/api/instructors");
Check(all.Status == 200 && all.Json.EnumerateArray().Select(i => i.GetProperty("fullName").GetString())
        .SequenceEqual(["Ada Yılmaz", "Burak Demir"]),
    "Instructor list is sorted by name");
var byName = await Get("/api/instructors?query=ADA");
Check(byName.Status == 200 && byName.Json.GetArrayLength() == 1 && byName.Json[0].GetProperty("instructorId").GetInt32() == adaId,
    "Instructor search by name ignores letter case");
var byDepartment = await Get("/api/instructors?departmentId=4");
Check(byDepartment.Status == 200 && byDepartment.Json.GetArrayLength() == 1 && byDepartment.Json[0].GetProperty("fullName").GetString() == "Burak Demir",
    "Instructor filter by department returns only that department");
var both = await Get("/api/instructors?query=ada&departmentId=4");
Check(both.Status == 200 && both.Json.GetArrayLength() == 0, "Name and department filters are combined; no match is an empty array");
Check((await Get("/api/instructors?departmentId=0")).Status == 400, "Instructor filter with department 0 is rejected (400)");

Check((await Put("/api/instructors/" + adaId, new { fullName = "Synthetic Name", departmentId = 5 }, adminToken)).Status == 200,
    "Instructor may move to another existing department without a minimum staffing rule");
var change = new { fullName = "Ada Yılmaz Öztürk", academicTitle = "Dr.", departmentId = 3 };
Check((await Put("/api/instructors/" + adaId, change)).Status == 401, "Edit instructor: anonymous is rejected (401)");
Check((await Put("/api/instructors/" + adaId, change, tokenOne)).Status == 403, "Edit instructor: a student is rejected (403)");
var changed = await Put("/api/instructors/" + adaId, change, adminToken);
Check(changed.Status == 200 && changed.Text("fullName") == "Ada Yılmaz Öztürk" && changed.Text("academicTitle") == "Dr."
      && changed.Json.GetProperty("departmentId").GetInt32() == 3,
    "Edit instructor: an administrator updates name and title in the existing department");
Check((await Put("/api/instructors/999999", change, adminToken)).Status == 404, "Edit instructor: an unknown id is not found (404)");
Check((await Put("/api/instructors/" + adaId, new { fullName = " ", departmentId = 5 }, adminToken)).Status == 400,
    "Edit instructor: a blank name is rejected (400)");
Check((await Put("/api/instructors/" + adaId, new { fullName = "Ada Yılmaz Öztürk", departmentId = 999 }, adminToken)).Status == 400,
    "Edit instructor: a department that does not exist is rejected (400)");
Check((await Send(HttpMethod.Delete, "/api/instructors/" + adaId, null, adminToken)).Status == 405,
    "Instructors cannot be deleted, even by an administrator (405)");

// ================================================================ what the detail pages read
var instructorDetail = await Get("/api/instructors/" + adaId);
Check(instructorDetail.Status == 200 && instructorDetail.Json.GetProperty("instructorId").GetInt32() == adaId
      && instructorDetail.Text("fullName") == "Ada Yılmaz Öztürk",
    "Instructor detail returns the requested record, without login");
Check(instructorDetail.PropertyNames().SetEquals(["instructorId", "fullName", "academicTitle", "departmentId"]),
    "Instructor detail has exactly the fields instructor-detail.html displays");
Check((await Get("/api/instructors/999999")).Status == 404, "Instructor detail: an unknown id is not found (404)");

var courseDetail = await Get("/api/courses/" + courseId);
Check(courseDetail.Status == 200 && courseDetail.Json.GetProperty("courseId").GetInt32() == courseId && courseDetail.Text("courseCode") == "CS101",
    "Course detail reads the existing course endpoint and gets the requested course");
Check(courseDetail.PropertyNames().IsSupersetOf(
        ["courseCode", "courseName", "isElective", "departmentId", "recommendedStudyYear", "courseCredit", "ects", "courseDescription"]),
    "The course response has every field course-detail.html displays");
Check((await Get("/api/courses/999999")).Status == 404, "Course detail: an unknown id is not found (404)");

// Course writes use the same trusted administrator role as the account module.
var newCourse = new { courseCode = "TEST101", courseName = "Synthetic Test", departmentId = 1, recommendedStudyYear = 1, courseCredit = 3, ects = 6 };
Check((await Post("/api/courses", newCourse)).Status == 401, "Course create rejects anonymous requests");
Check((await Post("/api/courses", newCourse, tokenOne)).Status == 403, "Course create rejects student requests");
var adminCourse = await Post("/api/courses", newCourse, adminToken);
Check(adminCourse.Status == 201, "Administrator can create a course");
var adminCourseId = adminCourse.Json.GetProperty("courseId").GetInt32();
var courseUpdate = new { courseId = adminCourseId, newCourse.courseCode, newCourse.courseName, newCourse.departmentId, newCourse.recommendedStudyYear, newCourse.courseCredit, newCourse.ects };
Check((await Put("/api/courses/" + adminCourseId, courseUpdate)).Status == 401, "Course update rejects anonymous requests");
Check((await Put("/api/courses/" + adminCourseId, courseUpdate, tokenOne)).Status == 403, "Course update rejects student requests");
Check((await Put("/api/courses/" + adminCourseId, courseUpdate, adminToken)).Status == 204, "Administrator can update a course");
Check((await Send(HttpMethod.Delete, "/api/courses/" + adminCourseId, null)).Status == 401, "Course delete rejects anonymous requests");
Check((await Send(HttpMethod.Delete, "/api/courses/" + adminCourseId, null, tokenOne)).Status == 403, "Course delete rejects student requests");
Check((await Send(HttpMethod.Delete, "/api/courses/" + adminCourseId, null, adminToken)).Status == 204, "Administrator can delete an unreferenced course");

// ================================================================ hand-over to review submission
// Student one is now in department 2, year 4 (changed through PUT /api/auth/me above).
var (reviewCourseId, programmeId) = await Db(async db =>
{
    var course = new Course { CourseCode = "CS402", CourseName = "Synthetic Seminar", DepartmentId = 4, IsActive = false };
    var programme = new Programme { ProgrammeKey = "TEST-P1", ProgrammeName = "Test Programme", DepartmentId = 2 };
    db.Courses.Add(course); db.Programmes.Add(programme);
    await db.SaveChangesAsync();
    db.ProgrammeCourses.Add(new ProgrammeCourse { ProgrammeId = programme.ProgrammeId, CourseId = course.CourseId,
        CatalogueYear = "2020-2021", RecommendedStudyYear = 4, IsElective = true });
    await db.SaveChangesAsync();
    return (course.CourseId, programme.ProgrammeId);
});
Check((await Put("/api/auth/me", new { fullName = "Student One", programmeId, studyYear = 4 }, tokenOne)).Status == 200,
    "Profile derives department from programme");
Check((await Put("/api/auth/me", new { fullName = "Student One", programmeId, departmentId = 1, studyYear = 4 }, tokenOne)).Status == 400,
    "Profile rejects mismatched programme and department");
Check((await Put("/api/auth/me", new { fullName = "Student One", programmeId = 999999, studyYear = 4 }, tokenOne)).Status == 400,
    "Profile rejects unknown programme");
Check((await Get("/api/programmes")).Status == 200, "Programme lookup is available during registration");
Check((await Get($"/api/programmes/{programmeId}/courses")).Status == 401, "Curriculum lookup requires login");
Check((await Get($"/api/programmes/{programmeId}/courses", tokenOne)).Json.GetArrayLength() == 1, "Curriculum includes historical inactive elective course");
var review = new { courseId = reviewCourseId, instructorId = adaId, academicYear = "2025-2026", semester = "Fall", attendanceConfirmed = true, reviewText = "Synthetic review text." };

Check((await Post("/api/reviews", review)).Status == 401, "Review: anonymous is rejected (401)");
Check((await Post("/api/reviews", review, tokenStaff)).Status == 403,
    "Review: an account without department and year can log in but cannot write a review (403)");
Check((await Post("/api/reviews", new { review.courseId, review.instructorId, review.academicYear, review.semester, attendanceConfirmed = false, review.reviewText }, tokenOne)).Status == 400,
    "Review requires explicit attendance confirmation");
Check((await Post("/api/reviews", new { review.courseId, instructorId = 999999, review.academicYear, review.semester, review.attendanceConfirmed, review.reviewText }, tokenOne)).Status == 400,
    "Review rejects nonexistent instructor");
Check((await Post("/api/reviews", new { review.courseId, review.instructorId, academicYear = "2025-2029", review.semester, review.attendanceConfirmed, review.reviewText }, tokenOne)).Status == 400,
    "Review rejects nonconsecutive academic years");
var submitted = await Post("/api/reviews", review, tokenOne);
Check(submitted.Status == 201 && submitted.Json.GetProperty("userId").GetInt32() == stored[EmailOne].UserId,
    "Review: historical cross-department elective accepts any existing instructor without offering and uses trusted author");
Check((await Post("/api/reviews", review, tokenOne)).Status == 409, "Review: a second review for the same course is rejected (409)");
Check((await Post("/api/reviews", new { review.courseId, review.academicYear, review.semester, review.attendanceConfirmed, review.instructorId, review.reviewText, userId = 1 }, adminToken)).Status == 400,
    "Review: a userId in the request body is rejected (400)");

// ================================================================ review moderation
var reviewId = submitted.Json.GetProperty("reviewId").GetInt32();
var reviewPath = $"/api/reviews/{reviewId}";
var moderationPath = reviewPath + "/moderation";
var courseReviews = $"/api/reviews?courseId={reviewCourseId}";
var hideReview = new { isHidden = true };

Check((await Get(reviewPath, tokenStaff)).Status == 200, "A second account can read a submitted review");
Check((await Put(moderationPath, hideReview)).Status == 401, "Moderation rejects anonymous callers");
Check((await Put(moderationPath, hideReview, tokenOne)).Status == 403, "Moderation rejects the review author");
Check((await Put(moderationPath, new { }, adminToken)).Status == 400, "Moderation requires a visibility value");
Check((await Put("/api/reviews/999999/moderation", hideReview, adminToken)).Status == 404,
    "Moderation rejects an unknown review");
Check((await Put(moderationPath, hideReview, adminToken)).Status == 204, "An administrator can hide a review");
Check((await Put(moderationPath, hideReview, adminToken)).Status == 204, "Repeating the same visibility change succeeds");
Check((await Get(courseReviews, tokenStaff)).Json.GetProperty("totalCount").GetInt32() == 0,
    "Hidden reviews are excluded from the filtered list and total count");
Check((await Get(reviewPath, tokenStaff)).Status == 404, "Hidden review details are unavailable to readers");
Check((await Get(courseReviews + "&includeHidden=true", tokenStaff)).Status == 403,
    "Only administrators can request the hidden review list");
var hiddenResults = (await Get(courseReviews + "&includeHidden=true", adminToken)).Json;
Check(hiddenResults.GetProperty("items")[0].GetProperty("isHidden").GetBoolean()
    && (await Get(reviewPath, adminToken)).Status == 200, "Administrators can find and open hidden reviews");
var moderation = await Db(db => db.Reviews.SingleAsync(row => row.ReviewId == reviewId));
Check(moderation.ModeratedAt.HasValue && moderation.ModeratedByUserId.HasValue,
    "Moderation records the acting administrator and time");
Check((await Post("/api/reviews", review, tokenOne)).Status == 409,
    "Hiding a review preserves the one-review-per-course rule");
Check((await Put(moderationPath, new { isHidden = false }, adminToken)).Status == 204
    && (await Get(reviewPath, tokenStaff)).Status == 200, "An administrator can restore a review for readers");

// ================================================================ catalogue v2 import
// Separate in-memory database: no records are inserted in the user's PostgreSQL.
using (var catalogConnection = new SqliteConnection("Data Source=:memory:"))
{
    await catalogConnection.OpenAsync();
    await using var catalogDb = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(catalogConnection).Options);
    await catalogDb.Database.EnsureCreatedAsync();
    var file = new CatalogFile
    {
        SchemaVersion = 2,
        Faculties = [new() { FacultyKey = "F1", FacultyName = "Synthetic Faculty" }, new() { FacultyKey = "F2", FacultyName = "Empty Faculty" }],
        Departments = [new() { DepartmentKey = "D1", FacultyKey = "F1", DepartmentName = "Department One" },
            new() { DepartmentKey = "D2", FacultyKey = "F1", DepartmentName = "Department Two", DepartmentCode = "SHARED" },
            new() { DepartmentKey = "D3", FacultyKey = "F1", DepartmentName = "Department Three", DepartmentCode = "SHARED" }],
        Instructors = [new() { InstructorKey = "I1", DepartmentKey = "D2", FullName = "Synthetic Instructor" }],
        Programmes = [new() { ProgrammeKey = "P1", DepartmentKey = "D1", ProgrammeName = "Programme One", DegreeLevel = "Bachelor", InstructionLanguage = "English" }],
        Courses = [new() { CourseKey = "C1", DepartmentKey = "D2", CourseCode = "X101", CourseName = "Cross Department Course" }],
        ProgrammeCourses = [new() { CurriculumKey = "PC1", ProgrammeKey = "P1", CourseKey = "C1", CatalogueYear = "2020-2021", Ects = 2.5m },
            new() { CurriculumKey = "PC2", ProgrammeKey = "P1", CourseKey = "C1", CatalogueYear = "2021-2022", RecommendedStudyYear = 2, IsElective = true }]
    };
    Check(CatalogFileValidator.Validate(file).Count == 0, "Catalog v2 accepts unknown/repeated official codes and empty academic units");
    var importer = new CatalogImportService(catalogDb);
    var imported = await importer.ImportAsync(file);
    Check(imported.Added == 10 && imported.Skipped == 0, "Catalog v2 imports all six entity types atomically");
    var repeatCatalog = await importer.ImportAsync(file);
    Check(repeatCatalog.Added == 0 && repeatCatalog.Skipped == 10, "Identical catalog reimport creates no duplicates");
    Check((await catalogDb.Departments.SingleAsync(d => d.DepartmentKey == "D1")).DepartmentCode is null,
        "Unknown official code remains null");
    Check(await catalogDb.Departments.CountAsync(d => d.DepartmentCode == "SHARED") == 2,
        "Repeated official codes retain separate department identities");
    var pc = await catalogDb.ProgrammeCourses.SingleAsync(p => p.CurriculumKey == "PC1");
    Check(pc.Ects == 2.5m && pc.IsElective is null && pc.RecommendedStudyYear is null,
        "Curriculum preserves fractional ECTS and unknown flags/year");
    Check(await catalogDb.ProgrammeCourses.CountAsync() == 2, "Repeated course occurrences across curricula are preserved");
    file.Faculties.Add(new() { FacultyKey = "F3", FacultyName = "Must Roll Back" });
    file.ProgrammeCourses[0]!.Ects = 9;
    var conflict = false;
    try { await importer.ImportAsync(file); } catch (CatalogImportConflictException) { conflict = true; }
    Check(conflict && !await catalogDb.Faculties.AnyAsync(f => f.ImportKey == "F3"), "Late curriculum conflict rolls back earlier faculty insertion");
    Check((await catalogDb.ProgrammeCourses.SingleAsync(p => p.CurriculumKey == "PC1")).Ects == 2.5m,
        "Conflicting import never overwrites existing curriculum");
    file.ProgrammeCourses[0]!.Ects = 2.5m;
    file.Instructors[0]!.DepartmentKey = "MISSING";
    Check(CatalogFileValidator.Validate(file).Count > 0, "Unresolved instructor department is rejected before import");
    file.Instructors[0]!.DepartmentKey = "D2";
    file.Programmes.Add(new() { ProgrammeKey = " p1 ", DepartmentKey = "D1", ProgrammeName = "Duplicate" });
    Check(CatalogFileValidator.Validate(file).Count > 0, "Stable keys are unique ignoring case and whitespace");
    file.Programmes.RemoveAt(1);
    file.SchemaVersion = 1;
    Check(CatalogFileValidator.Validate(file).Count > 0, "Legacy import contract cannot silently mislink departments");
}

// Empty units are supported through the administrator API, not just direct imports.
var emptyFaculty = await Post("/api/faculties", new { facultyName = "Empty API Faculty", departments = Array.Empty<object>() }, adminToken);
Check(emptyFaculty.Status == 201, "Faculty API accepts zero departments");
if (emptyFaculty.Status == 201)
{
    var facultyId = emptyFaculty.Json.GetProperty("facultyId").GetInt32();
    var emptyDepartment = await Post("/api/departments", new { facultyId, departmentName = "Empty API Department", instructors = Array.Empty<object>() }, adminToken);
    Check(emptyDepartment.Status == 201 && emptyDepartment.Json.GetProperty("departmentCode").ValueKind == JsonValueKind.Null,
        "Department API accepts zero instructors and unknown official code");
    if (emptyDepartment.Status == 201)
        Check((await Send(HttpMethod.Delete, "/api/departments/" + emptyDepartment.Json.GetProperty("departmentId").GetInt32(), null, adminToken)).Status == 204,
            "Last empty department may be deleted without inventing a replacement");
}

// ================================================================ administrator review deletion
var targetReviewId = submitted.Json.GetProperty("reviewId").GetInt32();
var targetReviewPath = $"/api/reviews/{targetReviewId}";
var courseReviewsPath = $"/api/reviews?courseId={reviewCourseId}&pageSize=1";
var otherAuthorId = stored[EmailStaff].UserId;
var (offeringId, otherReviewId) = await Db(async db =>
{
    var offering = new CourseOffering { CourseId = reviewCourseId, AcademicYear = review.academicYear,
        Semester = review.semester, Status = "Published" };
    db.CourseOfferings.Add(offering);
    await db.SaveChangesAsync();
    db.OfferingInstructors.Add(new OfferingInstructor { OfferingId = offering.OfferingId, InstructorId = adaId });
    await db.SaveChangesAsync();
    var target = await db.Reviews.SingleAsync(row => row.ReviewId == targetReviewId);
    target.OfferingId = offering.OfferingId;
    var other = new Review { UserId = otherAuthorId, CourseId = reviewCourseId, OfferingId = offering.OfferingId,
        InstructorId = adaId, ReviewText = "Another synthetic review.", CreatedAt = DateTime.UtcNow };
    db.Reviews.Add(other);
    await db.SaveChangesAsync();
    return (offering.OfferingId, other.ReviewId);
});

async Task CheckDeleteDenied(string? token, int expected, string label)
{
    var answer = await Send(HttpMethod.Delete, targetReviewPath, token: token);
    Check(answer.Status == expected && await Db(db => db.Reviews.AnyAsync(row => row.ReviewId == targetReviewId)), label);
}

await CheckDeleteDenied(null, 401, "Review delete: anonymous request gets 401 and leaves the review");
await CheckDeleteDenied(tokenStaff, 403, "Review delete: a different nonadministrator gets 403 and leaves the review");
await CheckDeleteDenied(tokenOne, 403, "Review delete: the nonadministrator author gets 403 and leaves the review");
var beforeDeletion = await Get(courseReviewsPath, tokenOne);
Check(beforeDeletion.Status == 200 && beforeDeletion.Json.GetProperty("totalCount").GetInt32() == 2,
    "Review delete: the course initially has two reviews");
var deletedReview = await Send(HttpMethod.Delete, targetReviewPath, token: adminToken);
Check(deletedReview.Status == 204 && deletedReview.Body.Length == 0
    && !await Db(db => db.Reviews.AnyAsync(row => row.ReviewId == targetReviewId)),
    "Review delete: administrator gets empty 204 and the target row is removed");
var repeatedDeletion = await Send(HttpMethod.Delete, targetReviewPath, token: adminToken);
Check(repeatedDeletion.Status == 404 && repeatedDeletion.Text("type") == "urn:udh:review-not-found",
    "Review delete: repeated deletion gets the specific review-not-found 404");
var afterDeletion = await Get(courseReviewsPath, tokenOne);
Check((await Get(targetReviewPath, tokenOne)).Status == 404 && afterDeletion.Status == 200
    && afterDeletion.Json.GetProperty("totalCount").GetInt32() == 1
    && afterDeletion.Json.GetProperty("items").EnumerateArray().Single().GetProperty("reviewId").GetInt32() == otherReviewId,
    "Review delete: detail disappears and the filtered list count and remaining item are correct");
Check(await Db(async db => await db.Reviews.AnyAsync(row => row.ReviewId == otherReviewId)
    && await db.UserAccounts.AnyAsync(row => row.UserId == stored[EmailOne].UserId)
    && await db.UserAccounts.AnyAsync(row => row.UserId == otherAuthorId)
    && await db.Courses.AnyAsync(row => row.CourseId == reviewCourseId)
    && await db.Instructors.AnyAsync(row => row.InstructorId == adaId)
    && await db.ProgrammeCourses.AnyAsync(row => row.ProgrammeId == programmeId && row.CourseId == reviewCourseId)
    && await db.CourseOfferings.AnyAsync(row => row.OfferingId == offeringId)
    && await db.OfferingInstructors.AnyAsync(row => row.OfferingId == offeringId && row.InstructorId == adaId)),
    "Review delete: other review, accounts, course, instructor, curriculum, offering and assignment are preserved");
Check((await Post("/api/reviews", review, tokenOne)).Status == 201,
    "Review delete: physical deletion releases the user-course slot under the current submission rule");

// ---------------------------------------------------------------- result
await app.StopAsync();
Console.WriteLine();
if (failures.Count > 0)
{
    Console.WriteLine($"{failures.Count} of {passed + failures.Count} checks FAILED:");
    foreach (var failure in failures) Console.WriteLine("  - " + failure);
    Environment.ExitCode = 1;
}
else
{
    Console.WriteLine($"{passed} checks passed. In-memory SQLite only; no PostgreSQL connection was made.");
}

// One HTTP answer: status code plus body, parsed as JSON when it is JSON.
sealed record Reply(int Status, string Body)
{
    public JsonElement Json { get; } = Parse(Body);

    private static JsonElement Parse(string body)
    {
        var text = body.TrimStart();
        if (!text.StartsWith('{') && !text.StartsWith('[')) return default;
        try { return JsonDocument.Parse(text).RootElement; }
        catch (JsonException) { return default; }
    }

    public string? Text(string property) =>
        Json.ValueKind == JsonValueKind.Object && Json.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    public HashSet<string> PropertyNames() =>
        Json.ValueKind == JsonValueKind.Object ? Json.EnumerateObject().Select(p => p.Name).ToHashSet() : [];

    // True when the body does not contain the password and no field at any depth is named like a
    // password, hash or normalized email. (Field names are checked, not the raw text: the token is
    // random characters and could contain "hash" by chance.)
    public bool HasNoPrivateFields(string password) =>
        Json.ValueKind != JsonValueKind.Undefined
        && !Body.Contains(password)
        && !AllPropertyNames(Json).Any(name =>
            name.Contains("password", StringComparison.OrdinalIgnoreCase)
            || name.Contains("hash", StringComparison.OrdinalIgnoreCase)
            || name.Contains("normalized", StringComparison.OrdinalIgnoreCase));

    private static IEnumerable<string> AllPropertyNames(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.Object => element.EnumerateObject().SelectMany(p => AllPropertyNames(p.Value).Prepend(p.Name)),
        JsonValueKind.Array => element.EnumerateArray().SelectMany(AllPropertyNames),
        _ => []
    };
}
