using System.Text.Json;
using UniversityDecisionHub.Api.Features.Reviews;
using UniversityDecisionHub.Api.Features.Reviews.Dtos;
using UniversityDecisionHub.Api.Features.Users;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using UniversityDecisionHub.Api.Common;
using UniversityDecisionHub.Api.Features.Departments;
using UniversityDecisionHub.Api.Features.Faculties;
using UniversityDecisionHub.Api.Features.Instructors;
using UniversityDecisionHub.Api.Features.Offerings;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using UniversityDecisionHub.Api.Data;
using UniversityDecisionHub.Api.Features.Courses;
using UniversityDecisionHub.Api.Features.Courses.Dtos;

// Model, validation and controller checks. No database is contacted.
var passed = 0;
void Check(bool condition, string name)
{
    if (!condition) throw new InvalidOperationException("FAIL: " + name);
    Console.WriteLine("PASS: " + name); passed++;
}
bool Valid(object dto) => Validator.TryValidateObject(dto, new ValidationContext(dto), [], true);
CourseCreateDto Input() => new() { CourseCode = "CS101", CourseName = "Computer Science", DepartmentId = 1,
    CourseCredit = 3, Ects = 6, RecommendedStudyYear = 1 };
Check(Valid(Input()), "Valid create request");
var input = Input(); input.RecommendedStudyYear = null;
Check(!Valid(input), "Missing year rejected");
input = Input(); input.RecommendedStudyYear = 0;
Check(!Valid(input), "Zero year rejected");
input = Input(); input.RecommendedStudyYear = -1;
Check(!Valid(input), "Negative year rejected");
input = Input(); input.DepartmentId = 0;
Check(!Valid(input), "Zero department rejected");
input = Input(); input.Ects = -1;
Check(!Valid(input), "Negative ECTS rejected");
input = Input(); input.CourseCredit = -1;
Check(!Valid(input), "Negative credits rejected");
input = Input(); input.CourseName = "   ";
Check(!Valid(input), "Blank course name rejected");
Check(!Valid(new CourseUpdateDto { CourseId = 1, CourseCode = "CS101", CourseName = "Computer Science",
    DepartmentId = 1, Ects = 6, CourseCredit = 3 }), "Update also requires explicit year");

using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
    .UseNpgsql("Host=localhost;Database=not_contacted;Username=not_used").Options);
var model = db.GetService<IDesignTimeModel>().Model;
var course = model.FindEntityType(typeof(Course))!;
Check(course.FindProperty(nameof(Course.RecommendedStudyYear))!.IsNullable, "Existing rows can retain unknown year");
Check(course.GetIndexes().Any(i => i.IsUnique && i.Properties.Select(p => p.Name)
    .SequenceEqual(new[] { "DepartmentId", "CourseCode" })), "Department and code remain a composite unique index");
Check(model.GetEntityTypes().Count() == 9, "Catalog, user profile and review entities are implemented");
var assembly = db.GetService<IMigrationsAssembly>();
Check(assembly.Migrations.Count == 4, "Course, catalog and review migrations are discoverable");
Check(!db.Database.HasPendingModelChanges(), "Migration snapshot matches current model");
var sql = db.GetService<IMigrator>().GenerateScript("20261013171125_InitialCourses", "20261022000000_AddRecommendedStudyYear");
Check(sql.Contains("ADD \"RecommendedStudyYear\" integer"), "Migration adds the nullable year column");
Check(!sql.Contains("DROP TABLE", StringComparison.OrdinalIgnoreCase) && !sql.Contains("DEFAULT", StringComparison.OrdinalIgnoreCase),
    "Migration neither drops tables nor invents existing course years");
Check(!Valid(new FacultyCreate { FacultyName = "Engineering" }), "Faculty requires a first department");
Check(!Valid(new DepartmentCreate { FacultyId = 1, DepartmentCode = "COME", DepartmentName = "Computing" }), "Department requires a first instructor");
Check(!Valid(new InstructorInput { FullName = "   " }), "Blank instructor name rejected");
Check(!Valid(new OfferingCreate { CourseId = 0, AcademicYear = "2026-2027", Semester = "Fall", Status = "Active" }), "Offering requires positive course ID");
Check(!Valid(new OfferingCreate { CourseId = 1, AcademicYear = " ", Semester = "Fall", Status = "Active" }), "Blank offering year rejected");
Check(Valid(new OfferingCreate { CourseId = 1, AcademicYear = "2026-2027", Semester = "Fall", Status = "Active" }), "Valid offering accepted");
Check(!Valid(new AssignmentInput()), "Assignment requires positive instructor ID");
var assignment = model.FindEntityType(typeof(OfferingInstructor))!;
Check(assignment.FindPrimaryKey()!.Properties.Select(p => p.Name).SequenceEqual(new[] { "OfferingId", "InstructorId" }), "Assignment pair prevents duplicate teachers per offering");
Check(assignment.GetForeignKeys().Count() == 2 && assignment.GetForeignKeys().All(f => f.DeleteBehavior == DeleteBehavior.Restrict), "Assignment has restricted offering and instructor FKs");
Check(course.GetForeignKeys().Any(f => f.PrincipalEntityType.ClrType == typeof(Department)), "Course references a department");
Check(model.FindEntityType(typeof(CourseOffering))!.GetForeignKeys().Single().DeleteBehavior == DeleteBehavior.Restrict, "Offerings prevent deleting their course");
Check(model.FindEntityType(typeof(Department))!.GetForeignKeys().Single().IsRequired, "Department requires a faculty");
Check(model.FindEntityType(typeof(Instructor))!.GetForeignKeys().Single().IsRequired, "Instructor requires a department");
var latest = assembly.Migrations.Keys.Last();
var catalogSql = db.GetService<IMigrator>().GenerateScript("20261022000000_AddRecommendedStudyYear", latest);
Check(catalogSql.Contains("NOT VALID"), "Legacy department references are preserved for later reconciliation");
Check(!catalogSql.Contains("DROP TABLE", StringComparison.OrdinalIgnoreCase), "Catalog upgrade drops no existing tables");

AuthorizationFilterContext AuthContext(ClaimsPrincipal principal)
{
    var http = new DefaultHttpContext { User = principal };
    return new AuthorizationFilterContext(new ActionContext(http, new RouteData(), new ActionDescriptor()), []);
}
var guard = new CatalogAdministratorAttribute();
var anonymous = AuthContext(new ClaimsPrincipal(new ClaimsIdentity())); guard.OnAuthorization(anonymous);
Check(anonymous.Result is UnauthorizedResult, "Anonymous catalog writes return 401 without an authentication handler");
var student = AuthContext(new ClaimsPrincipal(new ClaimsIdentity([], "test"))); guard.OnAuthorization(student);
Check(student.Result is StatusCodeResult { StatusCode: 403 }, "Nonadministrator catalog writes return 403");
var admin = AuthContext(new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Role, "Administrator")], "test"))); guard.OnAuthorization(admin);
Check(admin.Result is null, "Authenticated administrator passes the write guard");
foreach (var controller in new[] { typeof(FacultyController), typeof(DepartmentController), typeof(OfferingController) })
{
    var writes = controller.GetMethods().Where(m => m.GetCustomAttributes(typeof(HttpPostAttribute), true).Any()
        || m.GetCustomAttributes(typeof(HttpPutAttribute), true).Any() || m.GetCustomAttributes(typeof(HttpDeleteAttribute), true).Any());
    Check(writes.All(m => m.GetCustomAttributes(typeof(CatalogAdministratorAttribute), true).Any()), controller.Name + " protects every write endpoint");
}
var faculties = new FacultyController(db);
Check(await faculties.Post(new FacultyCreate { FacultyName = "Engineering", Departments = [] }, default) is BadRequestObjectResult,
    "Faculty controller refuses empty child collection before saving");
Check(await faculties.Post(new FacultyCreate { FacultyName = "Engineering", Departments = [new DepartmentSeed { DepartmentCode = "COME", DepartmentName = "Computing", Instructors = [] }] }, default) is BadRequestObjectResult,
    "Faculty controller refuses a department without instructors");
var departments = new DepartmentController(db);
Check(await departments.Post(new DepartmentCreate { FacultyId = 1, DepartmentCode = "COME", DepartmentName = "Computing", Instructors = [] }, default) is BadRequestObjectResult,
    "Department controller refuses missing instructor before querying the database");
Check(ReviewEligibility.Check(1, 3, 1, 3) is null, "Matching department and equal study year allow review");
Check(ReviewEligibility.Check(1, 4, 1, 3) is null, "Higher study year allows review");
Check(ReviewEligibility.Check(1, 2, 1, 3) is not null, "Lower study year rejected");
Check(ReviewEligibility.Check(2, 4, 1, 3) is not null, "Different department rejected");
Check(ReviewEligibility.Check(null, 4, 1, 3) is not null, "Missing student department rejected");
Check(ReviewEligibility.Check(1, null, 1, 3) is not null, "Missing student year rejected");
Check(ReviewEligibility.Check(1, 0, 1, 3) is not null, "Invalid student year rejected");
Check(ReviewEligibility.Check(1, 4, 1, null) is not null, "Unknown legacy course year does not grant eligibility");
Check(ReviewEligibility.Check(1, 4, 1, 0) is not null, "Invalid course year rejected");
ReviewCreateDto ReviewInput() => new() { CourseId = 1, OfferingId = 2, InstructorId = 3, ReviewText = "Useful course." };
Check(Valid(ReviewInput()), "Valid review input accepted");
var reviewInput = ReviewInput(); reviewInput.ReviewText = "   ";
Check(!Valid(reviewInput), "Blank review rejected");
reviewInput = ReviewInput(); reviewInput.OfferingId = 0;
Check(!Valid(reviewInput), "Invalid offering ID rejected");
reviewInput = ReviewInput(); reviewInput.InstructorId = -1;
Check(!Valid(reviewInput), "Invalid instructor ID rejected");
var rejectedAuthor = false;
try { JsonSerializer.Deserialize<ReviewCreateDto>("{\"courseId\":1,\"offeringId\":2,\"instructorId\":3,\"reviewText\":\"Hi\",\"userId\":999}", new JsonSerializerOptions(JsonSerializerDefaults.Web)); }
catch (JsonException) { rejectedAuthor = true; }
Check(rejectedAuthor, "Submitted author ID is rejected as an unmapped field");
var reviewModel = model.FindEntityType(typeof(Review))!;
Check(reviewModel.GetIndexes().Any(i => i.IsUnique && i.Properties.Select(p => p.Name).SequenceEqual(new[] { "UserId", "CourseId" })), "One review per user/course enforced across offerings");
Check(reviewModel.GetForeignKeys().Any(f => f.PrincipalEntityType.ClrType == typeof(UserAccount)), "Review has a real author foreign key");
Check(reviewModel.GetForeignKeys().Any(f => f.PrincipalEntityType.ClrType == typeof(CourseOffering)
    && f.Properties.Select(p => p.Name).SequenceEqual(new[] { "OfferingId", "CourseId" })
    && f.PrincipalKey.Properties.Select(p => p.Name).SequenceEqual(new[] { "OfferingId", "CourseId" })), "Review course must match the offering");
Check(reviewModel.GetForeignKeys().Any(f => f.PrincipalEntityType.ClrType == typeof(OfferingInstructor)
    && f.Properties.Select(p => p.Name).SequenceEqual(new[] { "OfferingId", "InstructorId" })), "Review instructor must match the assignment");
Check(reviewModel.GetForeignKeys().All(f => f.DeleteBehavior == DeleteBehavior.Restrict), "Referenced author/catalog records cannot silently cascade-delete reviews");
var service = new ReviewSubmissionService(db, TimeProvider.System);
Check((await service.SubmitAsync(new ClaimsPrincipal(new ClaimsIdentity()), ReviewInput(), default)).StatusCode == 401, "Service rejects anonymous caller before DB access");
var missingSubject = new ClaimsPrincipal(new ClaimsIdentity([], "test"));
Check((await service.SubmitAsync(missingSubject, ReviewInput(), default)).StatusCode == 401, "Authenticated identity without UserId is rejected");
var badSubject = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "not-an-id")], "test"));
Check((await service.SubmitAsync(badSubject, ReviewInput(), default)).StatusCode == 401, "Malformed authenticated UserId is rejected");
var trustedUser = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "1")], "test"));
Check((await service.SubmitAsync(trustedUser, new ReviewCreateDto(), default)).StatusCode == 400, "Service validates input independently of MVC");
var forged = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "1")]));
Check((await service.SubmitAsync(forged, ReviewInput(), default)).StatusCode == 401, "Untrusted identity claims cannot authenticate submission");
var reviewSql = db.GetService<IMigrator>().GenerateScript(assembly.Migrations.Keys.ElementAt(2), latest);
Check(reviewSql.Contains("CREATE TABLE \"Reviews\""), "Review migration creates review storage");
Check(!reviewSql.Contains("DROP TABLE", StringComparison.OrdinalIgnoreCase), "Review upgrade drops no existing tables");
Console.WriteLine($"{passed} checks passed. No database connection or migration application was performed.");
