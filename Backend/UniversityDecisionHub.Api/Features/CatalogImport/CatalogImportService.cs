using System.Data;
using Microsoft.EntityFrameworkCore;
using UniversityDecisionHub.Api.Data;
using UniversityDecisionHub.Api.Features.Faculties;
using UniversityDecisionHub.Api.Features.Departments;
using UniversityDecisionHub.Api.Features.Instructors;
using UniversityDecisionHub.Api.Features.Courses;
using UniversityDecisionHub.Api.Features.Programmes;
using static UniversityDecisionHub.Api.Features.CatalogImport.CatalogFileValidator;
namespace UniversityDecisionHub.Api.Features.CatalogImport;

public sealed record CatalogImportResult(int Added, int Skipped);
public sealed class CatalogImportConflictException(string message) : Exception(message);

public sealed class CatalogImportService(ApplicationDbContext db)
{
    public async Task<CatalogImportResult> ImportAsync(CatalogFile file, CancellationToken ct = default)
    {
        var errors = CatalogFileValidator.Validate(file);
        if (errors.Count != 0) throw new CatalogImportConflictException(string.Join(Environment.NewLine, errors));
        if (db.ChangeTracker.HasChanges()) throw new CatalogImportConflictException("Import requires a context without pending edits.");
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var added = 0;
        var skipped = 0;
        try
        {
            var faculties = await db.Faculties.ToListAsync(ct);
            var departments = await db.Departments.ToListAsync(ct);
            var instructors = await db.Instructors.ToListAsync(ct);
            var programmes = await db.Programmes.ToListAsync(ct);
            var courses = await db.Courses.ToListAsync(ct);
            var curricula = await db.ProgrammeCourses.ToListAsync(ct);
            var fm = new Dictionary<string, Faculty>();
            var dm = new Dictionary<string, Department>();
            var pm = new Dictionary<string, Programme>();
            var cm = new Dictionary<string, Course>();
            foreach (var r in file.Faculties!.Select(r => r!))
                fm.Add(Key(r.FacultyKey), Merge(faculties, new Faculty { ImportKey = Key(r.FacultyKey), FacultyName = r.FacultyName!.Trim() },
                    x => x.ImportKey, x => Key(x.FacultyName) == Key(r.FacultyName)));
            await db.SaveChangesAsync(ct);
            foreach (var r in file.Departments!.Select(r => r!))
            {
                var parent = fm[Key(r.FacultyKey)].FacultyId;
                dm.Add(Key(r.DepartmentKey), Merge(departments, new Department { DepartmentKey = Key(r.DepartmentKey),
                    FacultyId = parent, DepartmentCode = Optional(r.DepartmentCode), DepartmentName = r.DepartmentName!.Trim() },
                    x => x.DepartmentKey, x => x.FacultyId == parent && Key(x.DepartmentName) == Key(r.DepartmentName)));
            }
            await db.SaveChangesAsync(ct);
            foreach (var r in file.Instructors!.Select(r => r!))
                Merge(instructors, new Instructor { ImportKey = Key(r.InstructorKey), FullName = r.FullName!.Trim(),
                    AcademicTitle = Optional(r.AcademicTitle), DepartmentId = dm[Key(r.DepartmentKey)].DepartmentId },
                    x => x.ImportKey, x => x.ImportKey == null && Key(x.FullName) == Key(r.FullName));
            foreach (var r in file.Programmes!.Select(r => r!))
                pm.Add(Key(r.ProgrammeKey), Merge(programmes, new Programme { ProgrammeKey = Key(r.ProgrammeKey),
                    ProgrammeName = r.ProgrammeName!.Trim(), DepartmentId = dm[Key(r.DepartmentKey)].DepartmentId,
                    DegreeLevel = Optional(r.DegreeLevel), InstructionLanguage = Optional(r.InstructionLanguage), BolognaProgrammeId = Optional(r.BolognaProgrammeId) },
                    x => x.ProgrammeKey, _ => false));
            foreach (var r in file.Courses!.Select(r => r!))
            {
                var parent = dm[Key(r.DepartmentKey)].DepartmentId;
                cm.Add(Key(r.CourseKey), Merge(courses, new Course { CourseKey = Key(r.CourseKey), DepartmentId = parent,
                    CourseCode = r.CourseCode!.Trim(), CourseName = r.CourseName!.Trim(), CourseDescription = Optional(r.CourseDescription) },
                    x => x.CourseKey, x => x.DepartmentId == parent && Key(x.CourseCode) == Key(r.CourseCode)));
            }
            await db.SaveChangesAsync(ct);
            foreach (var r in file.ProgrammeCourses!.Select(r => r!))
                Merge(curricula, new ProgrammeCourse { CurriculumKey = Key(r.CurriculumKey),
                    ProgrammeId = pm[Key(r.ProgrammeKey)].ProgrammeId, CourseId = cm[Key(r.CourseKey)].CourseId,
                    CatalogueYear = Optional(r.CatalogueYear), Semester = r.Semester, RecommendedStudyYear = r.RecommendedStudyYear,
                    IsElective = r.IsElective, CourseCredit = r.CourseCredit, Ects = r.Ects }, x => x.CurriculumKey, _ => false);
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return new(added, skipped);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            db.ChangeTracker.Clear();
            throw;
        }

        T Merge<T>(List<T> rows, T desired, Func<T, string?> key, Func<T, bool> ambiguous) where T : class
        {
            var matches = rows.Where(x => Key(key(x)) == Key(key(desired))).ToArray();
            if (matches.Length > 1) throw new CatalogImportConflictException(typeof(T).Name + ": duplicate local source keys.");
            if (matches.Length == 1)
            {
                var existing = matches[0];
                // Compare mapped scalar fields, not navigation collections or generated local IDs.
                var differences = db.Entry(existing).Metadata.GetProperties()
                    .Where(p => !p.IsPrimaryKey() && !Equals(p.PropertyInfo!.GetValue(existing), p.PropertyInfo.GetValue(desired)))
                    .Select(p => p.Name).ToArray();
                if (differences.Length > 0)
                    throw new CatalogImportConflictException(typeof(T).Name + " key " + Key(key(desired)) +
                        ": existing values differ in " + string.Join(", ", differences) + ". Review changes explicitly; no overwrite performed.");
                skipped++;
                return existing;
            }
            if (rows.Any(ambiguous)) throw new CatalogImportConflictException(typeof(T).Name + ": ambiguous existing identity. Reconcile source keys before import.");
            db.Set<T>().Add(desired);
            rows.Add(desired);
            added++;
            return desired;
        }
    }
}
