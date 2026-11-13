using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniversityDecisionHub.Api.Data;
using UniversityDecisionHub.Api.Features.Reviews;
using UniversityDecisionHub.Api.Features.Reviews.Dtos;

// Standalone contract and SQL-generation checks. Does NOT open a database connection.
var passed = 0;

void Check(bool condition, string name)
{
    if (!condition) throw new InvalidOperationException("FAIL: " + name);
    Console.WriteLine("PASS: " + name);
    passed++;
}

bool Valid(ReviewReadQuery query) =>
    Validator.TryValidateObject(query, new ValidationContext(query), [], validateAllProperties: true);

var defaults = new ReviewReadQuery();
Check(defaults.Page == 1 && defaults.PageSize == 20, "Default pagination is page 1 / size 20");
Check(Valid(defaults), "Default search is valid");
Check(Valid(new ReviewReadQuery {
    CourseCode = "COME 327", DepartmentId = 1, InstructorId = 2,
    AcademicYear = "2026-2027", Page = 2, PageSize = 50
}), "All filters can be provided together");
Check(!Valid(new ReviewReadQuery { Page = 0 }), "Page zero rejected");
Check(!Valid(new ReviewReadQuery { Page = -1 }), "Negative page rejected");
Check(!Valid(new ReviewReadQuery { PageSize = 0 }), "Page size zero rejected");
Check(!Valid(new ReviewReadQuery { PageSize = 101 }), "Page size over 100 rejected");
Check(!Valid(new ReviewReadQuery { DepartmentId = 0 }), "Invalid department rejected");
Check(!Valid(new ReviewReadQuery { InstructorId = -2 }), "Invalid instructor rejected");
Check(!Valid(new ReviewReadQuery { CourseCode = new string('A', 65) }), "Overlong course code rejected");
Check(!Valid(new ReviewReadQuery { AcademicYear = "tomorrow" }), "Invalid academic-year format rejected");
Check(Valid(new ReviewReadQuery { AcademicYear = "2026-2027" }), "Valid academic-year format accepted");

var controllerType = typeof(ReviewReadingController);
Check(controllerType.IsDefined(typeof(AuthorizeAttribute), inherit: true), "All reading actions require authorization");
Check(controllerType.GetCustomAttribute<RouteAttribute>()?.Template == "api/reviews", "Reading route uses shared reviews base path");
var list = controllerType.GetMethod(nameof(ReviewReadingController.List))!;
var detail = controllerType.GetMethod(nameof(ReviewReadingController.Detail))!;
Check(list.GetCustomAttribute<HttpGetAttribute>() is not null, "List exposes GET method");
Check(detail.GetCustomAttribute<HttpGetAttribute>()?.Template == "{id:int}", "Detail exposes GET by integer id");
Check(controllerType.GetMethods(BindingFlags.Instance | BindingFlags.Public)
    .Where(m => m.DeclaringType == controllerType)
    .All(m => m.GetCustomAttributes<HttpPostAttribute>().Any() == false),
    "Reading controller does not duplicate Aygul's POST");
Check(typeof(ReviewSubmissionController).GetMethod("Post")?
    .GetCustomAttribute<HttpPostAttribute>() is not null, "Aygul's submission POST remains intact");

Check(typeof(PagedReviewsDto).GetProperty(nameof(PagedReviewsDto.TotalCount)) is not null
    && typeof(PagedReviewsDto).GetProperty(nameof(PagedReviewsDto.Items)) is not null,
    "Paged results expose total count and items");
Check(typeof(ReviewReadDto).GetProperty("UserId") is null
    && typeof(ReviewReadDto).GetProperty("Email") is null,
    "Reading DTO does not expose author identity or email");

// Creating a provider-backed IQueryable and generating SQL does not contact PostgreSQL.
using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
    .UseNpgsql("Host=localhost;Database=not_contacted;Username=not_used").Options);
var controller = new ReviewReadingController(db);
var method = controllerType.GetMethod("ProjectReviews", BindingFlags.NonPublic | BindingFlags.Instance);
Check(method is not null, "Projection query method exists");
var query = (IQueryable<ReviewReadDto>)method!.Invoke(controller, null)!;
var sql = query.ToQueryString();
Check(sql.Contains("\"Reviews\"", StringComparison.Ordinal)
    && sql.Contains("\"Courses\"", StringComparison.Ordinal)
    && sql.Contains("\"CourseOfferings\"", StringComparison.Ordinal)
    && sql.Contains("\"Instructors\"", StringComparison.Ordinal),
    "Reading projection translates to SQL joining review, course, offering and instructor");
Check(sql.Contains("\"ReviewText\"", StringComparison.Ordinal)
    && sql.Contains("\"AcademicYear\"", StringComparison.Ordinal),
    "SQL projection includes review text and academic year");


 // Deterministic examples verify actual filtering and paging behavior, not only DTO annotations.
var sampleReviews = new[]
{
    new ReviewReadDto { ReviewId = 1, CourseCode = "COME 327", DepartmentId = 1, InstructorId = 3,
        AcademicYear = "2026-2027", CreatedAt = new DateTime(2026, 10, 10) },
    new ReviewReadDto { ReviewId = 2, CourseCode = "COME 327", DepartmentId = 1, InstructorId = 3,
        AcademicYear = "2026-2027", CreatedAt = new DateTime(2026, 10, 11) },
    new ReviewReadDto { ReviewId = 3, CourseCode = "COME 327", DepartmentId = 1, InstructorId = 4,
        AcademicYear = "2025-2026", CreatedAt = new DateTime(2026, 10, 12) },
    new ReviewReadDto { ReviewId = 4, CourseCode = "COME 211", DepartmentId = 1, InstructorId = 3,
        AcademicYear = "2026-2027", CreatedAt = new DateTime(2026, 10, 13) },
    new ReviewReadDto { ReviewId = 5, CourseCode = "MATH 101", DepartmentId = 2, InstructorId = 5,
        AcademicYear = "2026-2027", CreatedAt = new DateTime(2026, 10, 14) },
    new ReviewReadDto { ReviewId = 6, CourseCode = "COME 327", DepartmentId = 1, InstructorId = 3,
        AcademicYear = "2026-2027", CreatedAt = new DateTime(2026, 10, 11) },
    new ReviewReadDto { ReviewId = 7, CourseCode = "SOME 333", DepartmentId = 2, InstructorId = 3,
        AcademicYear = "2026-2027", CreatedAt = new DateTime(2026, 10, 15) }
}.AsQueryable();

static int[] Ids(IQueryable<ReviewReadDto> rows) => rows.Select(r => r.ReviewId).ToArray();
static bool Matches(IQueryable<ReviewReadDto> rows, params int[] expected) =>
    Ids(rows).Order().SequenceEqual(expected.Order());

Check(Matches(ReviewReadFiltering.Apply(sampleReviews, new ReviewReadQuery { CourseCode = " come " }),
    1, 2, 3, 4, 6), "Course code search is trimmed, partial and case-insensitive");
Check(Matches(ReviewReadFiltering.Apply(sampleReviews, new ReviewReadQuery { DepartmentId = 1 }),
    1, 2, 3, 4, 6), "Department filter selects only matching reviews");
Check(Matches(ReviewReadFiltering.Apply(sampleReviews, new ReviewReadQuery { InstructorId = 3 }),
    1, 2, 4, 6, 7), "Instructor filter selects only matching reviews");
Check(Matches(ReviewReadFiltering.Apply(sampleReviews, new ReviewReadQuery { AcademicYear = "2025-2026" }),
    3), "Academic-year filter uses exact year");
var combined = new ReviewReadQuery {
    CourseCode = "COME 327", DepartmentId = 1, InstructorId = 3, AcademicYear = "2026-2027"
};
Check(Matches(ReviewReadFiltering.Apply(sampleReviews, combined), 1, 2, 6),
    "All four filters combine with AND");
Check(!ReviewReadFiltering.Apply(sampleReviews, new ReviewReadQuery { CourseCode = "UNKNOWN" }).Any(),
    "Unmatched search returns an empty collection");
Check(Ids(ReviewReadFiltering.Page(sampleReviews, new ReviewReadQuery { PageSize = 2 }))
    .SequenceEqual(new[] { 7, 5 }), "First page returns newest two reviews");
Check(Ids(ReviewReadFiltering.Page(sampleReviews, new ReviewReadQuery { Page = 2, PageSize = 2 }))
    .SequenceEqual(new[] { 4, 3 }), "Second page returns next two reviews");
Check(Ids(ReviewReadFiltering.Page(sampleReviews, new ReviewReadQuery { Page = 3, PageSize = 2 }))
    .SequenceEqual(new[] { 6, 2 }), "Same-timestamp reviews use descending review ID as tiebreaker");
Check(Ids(ReviewReadFiltering.Page(sampleReviews, new ReviewReadQuery { Page = 10, PageSize = 2 }))
    .Length == 0, "Page beyond results is empty");

var filteredSql = ReviewReadFiltering.Page(
    ReviewReadFiltering.Apply(query, combined),
    new ReviewReadQuery { Page = 2, PageSize = 2 }).ToQueryString();
Check(filteredSql.Contains("WHERE", StringComparison.OrdinalIgnoreCase)
    && filteredSql.Contains("ORDER BY", StringComparison.OrdinalIgnoreCase)
    && filteredSql.Contains("LIMIT", StringComparison.OrdinalIgnoreCase)
    && filteredSql.Contains("OFFSET", StringComparison.OrdinalIgnoreCase),
    "Combined PostgreSQL query translates filters, sorting and paging without connecting");

Console.WriteLine($"{passed} Review Reading checks passed. No database connection or migration application was performed.");
