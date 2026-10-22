using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using UniversityDecisionHub.Api.Data;
using UniversityDecisionHub.Api.Features.Courses;
using UniversityDecisionHub.Api.Features.Courses.Dtos;

// Dependency-free checks for this small course-only change. No DB is contacted.
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
Check(model.GetEntityTypes().Count() == 2, "Only the two original course entities are implemented");
var assembly = db.GetService<IMigrationsAssembly>();
Check(assembly.Migrations.Count == 2, "Initial and additive course migrations are discoverable");
Check(!db.Database.HasPendingModelChanges(), "Migration snapshot matches current model");
var sql = db.GetService<IMigrator>().GenerateScript("20261013171125_InitialCourses", "20261022000000_AddRecommendedStudyYear");
Check(sql.Contains("ADD \"RecommendedStudyYear\" integer"), "Migration adds the nullable year column");
Check(!sql.Contains("DROP TABLE", StringComparison.OrdinalIgnoreCase) && !sql.Contains("DEFAULT", StringComparison.OrdinalIgnoreCase),
    "Migration neither drops tables nor invents existing course years");
Console.WriteLine($"{passed} checks passed. No database connection or migration application was performed.");
