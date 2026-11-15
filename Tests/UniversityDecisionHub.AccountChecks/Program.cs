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
    db.UserAccounts.AddRange(
        Listed("Yiğit Şahin", EmailOne, 1, 3),
        Listed("Student Two", EmailTwo, 2, 4),
        Listed("Never Registered", EmailNever, 1, 1),
        Listed("Staff Member", EmailStaff, null, null)); // no student profile
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
// auth request 1/10 — name typed without diacritics, in another letter case and with extra spaces
var register = await Post("/api/auth/register", new
{
    fullName = "yigit   SAHIN", email = "Yigit.Sahin@Example.com", departmentId = 1, studyYear = 3, password = PasswordOne
});
Check(register.Status == 201, "Registration that matches a pre-defined user succeeds");
var tokenOne = register.Text("token") ?? "";
Check(tokenOne.Length > 0, "Registration returns a token");
Check(register.Json.TryGetProperty("user", out var registeredUser)
      && registeredUser.GetProperty("fullName").GetString() == "Yiğit Şahin"
      && registeredUser.GetProperty("email").GetString() == EmailOne,
    "Registration answers with the stored name and email, not the typed ones");
Check(register.HasNoPrivateFields(PasswordOne), "Registration response has no password, hash or normalized email");

// auth request 2/10
var again = await Post("/api/auth/register", new
{
    fullName = "Yiğit Şahin", email = EmailOne, departmentId = 1, studyYear = 3, password = "another-password"
});
Check(again.Status == 409, "A second registration for the same account is rejected (409)");

// auth request 3/10
var wrongYear = await Post("/api/auth/register", new
{
    fullName = "Student Two", email = EmailTwo, departmentId = 2, studyYear = 1, password = PasswordTwo
});
Check(wrongYear.Status == 404, "Registration with a different study year is rejected (404)");

// auth request 4/10
var unknownEmail = await Post("/api/auth/register", new
{
    fullName = "Nobody Here", email = "nobody@example.com", departmentId = 2, studyYear = 4, password = PasswordTwo
});
Check(unknownEmail.Status == 404, "Registration with an email that is not on the list is rejected (404)");
Check(unknownEmail.Body == wrongYear.Body, "Unknown email and wrong detail give the same answer");

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

// auth request 6/10 — an account without department and year completes registration and is logged in
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
Check(me.PropertyNames().SetEquals(["userId", "fullName", "email", "departmentId", "programmeId", "studyYear", "isAdministrator"]),
    "The profile contains exactly the six public fields");

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
Check(users.Status == 200 && users.Json.GetArrayLength() == 4, "User list: an administrator sees all four accounts");
Check(users.HasNoPrivateFields(PasswordOne), "User list has no password, hash or normalized email");
Check(users.Json.EnumerateArray().Single(u => u.GetProperty("email").GetString() == EmailNever)
        .GetProperty("hasCompletedRegistration").GetBoolean() == false,
    "User list shows who has not completed registration yet");

var newUser = new { fullName = " New Person ", email = "new.person@example.com", departmentId = 1, studyYear = 2, isAdministrator = true, password = "ignored-value" };
Check((await Post("/api/admin/users", newUser)).Status == 401, "Add user: anonymous is rejected (401)");
Check((await Post("/api/admin/users", newUser, tokenOne)).Status == 403, "Add user: a student is rejected (403)");
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
Check(await Db(db => db.UserAccounts.CountAsync()) == 7, "Seven accounts exist: four listed, one added, two imported");

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

// Read a newly submitted review and a review written before the direct period columns existed.
var legacyReviewId = await Db(async db =>
{
    var offering = new CourseOffering
    {
        CourseId = reviewCourseId, AcademicYear = "2024-2025", Semester = "Spring", Status = "Completed"
    };
    db.CourseOfferings.Add(offering);
    await db.SaveChangesAsync();
    db.OfferingInstructors.Add(new OfferingInstructor { OfferingId = offering.OfferingId, InstructorId = adaId });
    var legacyReview = new Review
    {
        UserId = stored[EmailNever].UserId, CourseId = reviewCourseId, OfferingId = offering.OfferingId,
        InstructorId = adaId, ReviewText = "Legacy review.", CreatedAt = DateTime.UtcNow
    };
    db.Reviews.Add(legacyReview);
    await db.SaveChangesAsync();
    return legacyReview.ReviewId;
});

var reviewId = submitted.Status == 201 ? submitted.Json.GetProperty("reviewId").GetInt32() : 0;
var reviewLoginToken = login.Text("token");
var reviewPath = $"/api/reviews/{reviewId}";
Check((await Get("/api/reviews")).Status == 401, "Review list rejects anonymous readers");
Check((await Get(reviewPath)).Status == 401, "Review detail rejects anonymous readers");

var bothReviews = await Get($"/api/reviews?courseCode=CS402&instructorId={adaId}", reviewLoginToken);
Check(bothReviews.Status == 200 && bothReviews.Json.GetProperty("items").EnumerateArray()
    .Select(item => item.GetProperty("reviewId").GetInt32()).ToHashSet().SetEquals([reviewId, legacyReviewId]),
    "Reading the course lists both the legacy review and the review without an offering");

var reviewList = await Get("/api/reviews?academicYear=2025-2026&semester=Fall", reviewLoginToken);
Check(reviewList.Status == 200 && reviewList.Json.GetProperty("totalCount").GetInt32() == 1
    && reviewList.Json.GetProperty("items").GetArrayLength() == 1
    && reviewList.Json.GetProperty("items")[0].GetProperty("reviewId").GetInt32() == reviewId,
    "Year and semester filters find the submitted review without an offering");

var reviewDetail = await Get(reviewPath, reviewLoginToken);
Check(reviewDetail.Status == 200 && reviewDetail.Text("reviewText") == review.reviewText
    && reviewDetail.Text("academicYear") == review.academicYear && reviewDetail.Text("semester") == review.semester
    && reviewDetail.Json.GetProperty("offeringId").ValueKind == JsonValueKind.Null,
    "Review detail returns the submitted text and declared period without an offering");

var legacyList = await Get("/api/reviews?academicYear=2024-2025&semester=Spring", reviewLoginToken);
Check(legacyList.Status == 200 && legacyList.Json.GetProperty("totalCount").GetInt32() == 1
    && legacyList.Json.GetProperty("items").GetArrayLength() == 1
    && legacyList.Json.GetProperty("items")[0].GetProperty("reviewId").GetInt32() == legacyReviewId,
    "Year and semester filters include a legacy review via its offering period");

var legacyDetail = await Get($"/api/reviews/{legacyReviewId}", reviewLoginToken);
Check(legacyDetail.Status == 200 && legacyDetail.Text("academicYear") == "2024-2025"
    && legacyDetail.Text("semester") == "Spring", "Legacy detail preserves its offering period");
Check((await Get("/api/reviews?semester=Winter", reviewLoginToken)).Status == 400,
    "Review list rejects unsupported semesters");

var exactCourseId = await Db(async db =>
{
    var exactCourse = new Course { CourseCode = "MATH 101", CourseName = "Test Course", DepartmentId = 4 };
    var longerCode = new Course { CourseCode = "MATH 1010", CourseName = "Test Course II", DepartmentId = 4 };
    var otherDepartment = new Course { CourseCode = "MATH 101", CourseName = "Test Course III", DepartmentId = 2 };
    db.Courses.AddRange(exactCourse, longerCode, otherDepartment);
    await db.SaveChangesAsync();
    foreach (var course in new[] { exactCourse, longerCode, otherDepartment })
    {
        db.Reviews.Add(new Review
        {
            UserId = stored[EmailOne].UserId, CourseId = course.CourseId, InstructorId = adaId,
            AcademicYear = "2025-2026", Semester = "Fall", ReviewText = course.CourseName, CreatedAt = DateTime.UtcNow
        });
    }
    await db.SaveChangesAsync();
    return exactCourse.CourseId;
});
var courseCodeList = await Get("/api/reviews?courseCode=MATH%20101&departmentId=4", reviewLoginToken);
Check(courseCodeList.Status == 200 && courseCodeList.Json.GetProperty("totalCount").GetInt32() == 2,
    "Manual course code search retains partial matching within a department");
var exactCourseList = await Get($"/api/reviews?courseId={exactCourseId}&courseCode=MATH%20101&departmentId=4", reviewLoginToken);
Check(exactCourseList.Status == 200 && exactCourseList.Json.GetProperty("totalCount").GetInt32() == 1
    && exactCourseList.Json.GetProperty("items")[0].GetProperty("courseId").GetInt32() == exactCourseId,
    "Course detail filters return only the exact course ID despite similar course codes");
var exactIdList = await Get($"/api/reviews?courseId={exactCourseId}", reviewLoginToken);
Check(exactIdList.Status == 200 && exactIdList.Json.GetProperty("totalCount").GetInt32() == 1
    && exactIdList.Json.GetProperty("items")[0].GetProperty("courseId").GetInt32() == exactCourseId,
    "Course ID alone excludes the same course code in another department");
Check((await Get("/api/reviews?courseId=0", reviewLoginToken)).Status == 400,
    "Review list rejects a nonpositive course ID");

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
