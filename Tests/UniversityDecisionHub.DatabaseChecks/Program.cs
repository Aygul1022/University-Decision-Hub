using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using UniversityDecisionHub.Api.Data;
using UniversityDecisionHub.Api.Features.Courses;
using UniversityDecisionHub.Api.Features.Departments;
using UniversityDecisionHub.Api.Features.Faculties;
using UniversityDecisionHub.Api.Features.Instructors;
using UniversityDecisionHub.Api.Features.Offerings;
using UniversityDecisionHub.Api.Features.Reviews;
using UniversityDecisionHub.Api.Features.Users;

// Database constraint checks and report-query checks (Ozan's part of the database work).
//
//   dotnet run --project Tests/UniversityDecisionHub.DatabaseChecks
//
// The tables, keys, unique indexes and foreign keys are created from the EF model in an in-memory
// SQLite database, then every write below goes straight to that database. This shows that the rules
// are enforced by the database itself and not only by the controllers.
//
// Limit of this check: SQLite is not PostgreSQL. The same keys and indexes are created from the same
// model, but the migrations are not executed here. Repeat the report queries on a migrated PostgreSQL
// database before quoting their output in the report. All data below is synthetic.

var passed = 0;
var failures = new List<string>();
void Check(bool condition, string name)
{
    if (condition) { passed++; Console.WriteLine("PASS: " + name); }
    else { failures.Add(name); Console.WriteLine("FAIL: " + name); }
}

using var connection = new SqliteConnection("Data Source=:memory:");
connection.Open();
var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
ApplicationDbContext NewDb() => new(options);

// True when the database refuses the write. A fresh context is used each time, so one refused
// write cannot affect the next check.
async Task<bool> Rejected(Func<ApplicationDbContext, Task> write)
{
    using var db = NewDb();
    try { await write(db); return false; }
    catch (Exception ex) when (ex is DbUpdateException or SqliteException) { return true; }
}

async Task<bool> Accepted(Func<ApplicationDbContext, Task> write) => !await Rejected(write);

async Task<T> Read<T>(Func<ApplicationDbContext, Task<T>> query)
{
    using var db = NewDb();
    return await query(db);
}

UserAccount User(int id, string name, string email, int? departmentId, int? studyYear) => new()
{
    UserId = id, FullName = name, Email = email, NormalizedEmail = UserAccount.NormalizeEmail(email),
    DepartmentId = departmentId, StudyYear = studyYear, CreatedAtUtc = DateTime.UtcNow
};
Course NewCourse(int id, string code, string name, int departmentId, int year) => new()
{
    CourseId = id, CourseCode = code, CourseName = name, DepartmentId = departmentId,
    RecommendedStudyYear = year, CourseCredit = 3, Ects = 6, IsActive = true
};
CourseOffering Offering(int id, int courseId, string academicYear, string semester) => new()
{
    OfferingId = id, CourseId = courseId, AcademicYear = academicYear, Semester = semester, Status = "Completed"
};
Review NewReview(int userId, int courseId, int offeringId, int instructorId, int day) => new()
{
    UserId = userId, CourseId = courseId, OfferingId = offeringId, InstructorId = instructorId,
    ReviewText = "Synthetic review text.", CreatedAt = new DateTime(2026, 1, day, 12, 0, 0, DateTimeKind.Utc)
};

// ---------------------------------------------------------------- synthetic data
using (var db = NewDb())
{
    await db.Database.EnsureCreatedAsync();

    db.Faculties.Add(new Faculty { FacultyId = 1, FacultyName = "Synthetic Faculty of Engineering" });
    await db.SaveChangesAsync();
    db.Departments.AddRange(
        new Department { DepartmentId = 1, FacultyId = 1, DepartmentCode = "CENG", DepartmentName = "Computer Engineering" },
        new Department { DepartmentId = 2, FacultyId = 1, DepartmentCode = "EE", DepartmentName = "Electrical Engineering" },
        new Department { DepartmentId = 3, FacultyId = 1, DepartmentCode = "ME", DepartmentName = "Mechanical Engineering" });
    await db.SaveChangesAsync();
    db.Courses.AddRange(
        NewCourse(1, "CENG301", "Synthetic Algorithms", 1, 3),
        NewCourse(2, "CENG101", "Synthetic Programming", 1, 1),
        NewCourse(3, "EE201", "Synthetic Circuits", 2, 2));
    db.Instructors.AddRange(
        new Instructor { InstructorId = 1, FullName = "Ada Synthetic", AcademicTitle = "Prof. Dr.", DepartmentId = 1 },
        new Instructor { InstructorId = 2, FullName = "Burak Synthetic", DepartmentId = 1 },
        new Instructor { InstructorId = 3, FullName = "Cem Synthetic", DepartmentId = 2 });
    db.UserAccounts.AddRange(
        User(1, "Student A", "student.a@example.com", 1, 3),
        User(2, "Student B", "student.b@example.com", 1, 4),
        User(3, "Student C", "student.c@example.com", 2, 2));
    await db.SaveChangesAsync();
    db.CourseOfferings.AddRange(
        Offering(1, 1, "2025-2026", "Fall"),
        Offering(2, 1, "2024-2025", "Fall"),
        Offering(3, 2, "2025-2026", "Fall"),
        Offering(4, 3, "2025-2026", "Spring"));
    await db.SaveChangesAsync();
    db.OfferingInstructors.AddRange(
        new OfferingInstructor { OfferingId = 1, InstructorId = 1 },
        new OfferingInstructor { OfferingId = 1, InstructorId = 2 },
        new OfferingInstructor { OfferingId = 2, InstructorId = 1 },
        new OfferingInstructor { OfferingId = 3, InstructorId = 2 },
        new OfferingInstructor { OfferingId = 4, InstructorId = 3 });
    await db.SaveChangesAsync();
    db.Reviews.AddRange(
        NewReview(userId: 1, courseId: 1, offeringId: 1, instructorId: 1, day: 10),
        NewReview(userId: 2, courseId: 1, offeringId: 2, instructorId: 1, day: 11),
        NewReview(userId: 2, courseId: 2, offeringId: 3, instructorId: 2, day: 12),
        NewReview(userId: 3, courseId: 3, offeringId: 4, instructorId: 3, day: 13));
    await db.SaveChangesAsync();
}
Console.WriteLine("Synthetic data loaded: 3 departments, 3 courses, 3 instructors, 4 offerings, 3 users, 4 reviews.");
Console.WriteLine();

// ================================================================ 1. duplicate course code
Check(await Rejected(async db => { db.Courses.Add(NewCourse(10, "CENG301", "Copy", 1, 3)); await db.SaveChangesAsync(); }),
    "A second course with the same code in the same department is rejected");
Check(await Accepted(async db => { db.Courses.Add(NewCourse(11, "CENG301", "Same code elsewhere", 2, 3)); await db.SaveChangesAsync(); }),
    "The same course code in another department is accepted");
Check(await Rejected(async db => { db.Departments.Add(new Department { DepartmentId = 9, FacultyId = 1, DepartmentCode = "CENG", DepartmentName = "Copy" }); await db.SaveChangesAsync(); }),
    "A second department with the same department code is rejected");

// ================================================================ 2. foreign keys to missing records
Check(await Rejected(async db => { db.Courses.Add(NewCourse(12, "XX100", "No department", 999, 1)); await db.SaveChangesAsync(); }),
    "A course cannot point to a department that does not exist");
Check(await Rejected(async db => { db.Instructors.Add(new Instructor { InstructorId = 12, FullName = "No Department", DepartmentId = 999 }); await db.SaveChangesAsync(); }),
    "An instructor cannot point to a department that does not exist");
Check(await Rejected(async db => { db.UserAccounts.Add(User(12, "No Department", "no.department@example.com", 999, 1)); await db.SaveChangesAsync(); }),
    "A user cannot point to a department that does not exist");
Check(await Accepted(async db => { db.UserAccounts.Add(User(13, "No Profile", "no.profile@example.com", null, null)); await db.SaveChangesAsync(); }),
    "A user without department and study year is accepted");
Check(await Rejected(async db => { db.Departments.Add(new Department { DepartmentId = 12, FacultyId = 999, DepartmentCode = "NOFAC", DepartmentName = "No faculty" }); await db.SaveChangesAsync(); }),
    "A department cannot point to a faculty that does not exist");
Check(await Rejected(async db => { db.CourseOfferings.Add(Offering(12, 999, "2025-2026", "Fall")); await db.SaveChangesAsync(); }),
    "An offering cannot point to a course that does not exist");
Check(await Rejected(async db => { db.OfferingInstructors.Add(new OfferingInstructor { OfferingId = 1, InstructorId = 999 }); await db.SaveChangesAsync(); }),
    "An assignment cannot point to an instructor that does not exist");
Check(await Rejected(async db => { db.OfferingInstructors.Add(new OfferingInstructor { OfferingId = 999, InstructorId = 1 }); await db.SaveChangesAsync(); }),
    "An assignment cannot point to an offering that does not exist");
Check(await Rejected(async db => { db.OfferingInstructors.Add(new OfferingInstructor { OfferingId = 1, InstructorId = 1 }); await db.SaveChangesAsync(); }),
    "The same instructor cannot be assigned to the same offering twice");
Check(await Rejected(async db => { db.Reviews.Add(NewReview(userId: 999, courseId: 2, offeringId: 3, instructorId: 2, day: 20)); await db.SaveChangesAsync(); }),
    "A review cannot point to a user that does not exist");

// ================================================================ 3. one review per user and course
Check(await Rejected(async db => { db.Reviews.Add(NewReview(userId: 1, courseId: 1, offeringId: 1, instructorId: 1, day: 21)); await db.SaveChangesAsync(); }),
    "The same user cannot add a second review for the same course");
Check(await Rejected(async db => { db.Reviews.Add(NewReview(userId: 1, courseId: 1, offeringId: 2, instructorId: 1, day: 22)); await db.SaveChangesAsync(); }),
    "...not even for another offering of that course");
Check(await Accepted(async db => { db.Reviews.Add(NewReview(userId: 1, courseId: 2, offeringId: 3, instructorId: 2, day: 23)); await db.SaveChangesAsync(); }),
    "The same user can review a different course");

// ================================================================ 4. review context must be consistent
Check(await Rejected(async db => { db.Reviews.Add(NewReview(userId: 3, courseId: 2, offeringId: 3, instructorId: 1, day: 24)); await db.SaveChangesAsync(); }),
    "A review cannot name an instructor who is not assigned to that offering");
Check(await Rejected(async db => { db.Reviews.Add(NewReview(userId: 3, courseId: 2, offeringId: 1, instructorId: 1, day: 25)); await db.SaveChangesAsync(); }),
    "A review cannot combine a course with an offering of another course");

// ================================================================ 5. one email, one account
Check(await Rejected(async db => { db.UserAccounts.Add(User(14, "Copy", "  STUDENT.A@Example.com ", 1, 1)); await db.SaveChangesAsync(); }),
    "A second account with the same email in another letter case is rejected");

// ================================================================ 6. deleting keeps references intact
Check(await Rejected(db => db.Faculties.Where(f => f.FacultyId == 1).ExecuteDeleteAsync()),
    "A faculty that has departments cannot be deleted");
Check(await Rejected(db => db.Departments.Where(d => d.DepartmentId == 1).ExecuteDeleteAsync()),
    "A department that has courses, instructors and users cannot be deleted");
Check(await Rejected(db => db.Courses.Where(c => c.CourseId == 1).ExecuteDeleteAsync()),
    "A course that has offerings and reviews cannot be deleted");
Check(await Rejected(db => db.CourseOfferings.Where(o => o.OfferingId == 1).ExecuteDeleteAsync()),
    "An offering that has assignments and reviews cannot be deleted");
Check(await Rejected(db => db.Instructors.Where(i => i.InstructorId == 1).ExecuteDeleteAsync()),
    "An instructor who is assigned to an offering cannot be deleted");
Check(await Rejected(db => db.OfferingInstructors.Where(a => a.OfferingId == 1 && a.InstructorId == 1).ExecuteDeleteAsync()),
    "An assignment that a review refers to cannot be deleted");
Check(await Rejected(db => db.UserAccounts.Where(u => u.UserId == 1).ExecuteDeleteAsync()),
    "A user who wrote a review cannot be deleted");
Check(await Accepted(db => db.Departments.Where(d => d.DepartmentId == 3).ExecuteDeleteAsync()),
    "A department that nothing refers to can be deleted");
Check(await Read(db => db.Reviews.CountAsync()) == 5 && await Read(db => db.OfferingInstructors.CountAsync()) == 5
      && await Read(db => db.CourseOfferings.CountAsync()) == 4 && await Read(db => db.Instructors.CountAsync()) == 3,
    "After the refused deletes every review, assignment, offering and instructor is still there");

// ================================================================ 7. report queries
// Restore the exact synthetic data set the expected row counts below were written for.
await Read(db => db.Reviews.Where(r => r.UserId == 1 && r.CourseId == 2).ExecuteDeleteAsync());
await Read(db => db.Courses.Where(c => c.CourseId == 11).ExecuteDeleteAsync());

var sqlPath = Path.Combine(AppContext.BaseDirectory, "report_queries.sql");
var statements = string.Join('\n', File.ReadAllLines(sqlPath).Where(line => !line.TrimStart().StartsWith("--")))
    .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

(string Name, int Rows)[] expected =
[
    ("Q1 courses by department", 3),
    ("Q2 number of courses in each department", 2),
    ("Q3 reviews of course CENG301", 2),
    ("Q4 reviews of the courses taught by instructor 1", 2),
    ("Q5 number of reviews in each academic year", 2),
    ("Q6 reviews written for 2025-2026", 3),
    ("Q7 instructors who teach offering 1", 2),
    ("Q8 combined filters: CENG301, instructor 1, 2025-2026", 1),
    ("Q9 users with more than one review for a course", 0),
];
Check(statements.Length == expected.Length, $"report_queries.sql contains {expected.Length} statements");

for (var i = 0; i < Math.Min(statements.Length, expected.Length); i++)
{
    int rows;
    try
    {
        using var command = connection.CreateCommand();
        command.CommandText = statements[i];
        using var reader = command.ExecuteReader();
        rows = 0;
        while (reader.Read()) rows++;
    }
    catch (SqliteException ex)
    {
        Check(false, $"{expected[i].Name}: the statement runs ({ex.Message})");
        continue;
    }
    Check(rows == expected[i].Rows, $"{expected[i].Name}: {expected[i].Rows} row(s) expected, {rows} returned");
}

// ---------------------------------------------------------------- result
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
