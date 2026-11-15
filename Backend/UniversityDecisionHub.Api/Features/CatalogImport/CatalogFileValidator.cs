using System.ComponentModel.DataAnnotations;
namespace UniversityDecisionHub.Api.Features.CatalogImport;

public static class CatalogFileValidator
{
    public static List<string> Validate(CatalogFile file)
    {
        var errors = new List<string>();
        if (file.SchemaVersion != 2) errors.Add("SchemaVersion must be 2. Legacy DepartmentCode links must be explicitly converted to DepartmentKey.");
        var faculties = Rows(file.Faculties, r => r.FacultyKey, "Faculties");
        var departments = Rows(file.Departments, r => r.DepartmentKey, "Departments");
        var instructors = Rows(file.Instructors, r => r.InstructorKey, "Instructors");
        var programmes = Rows(file.Programmes, r => r.ProgrammeKey, "Programmes");
        var courses = Rows(file.Courses, r => r.CourseKey, "Courses");
        Rows(file.ProgrammeCourses, r => r.CurriculumKey, "ProgrammeCourses");
        Links(file.Departments, r => r.FacultyKey, faculties, "Departments.FacultyKey");
        Links(file.Instructors, r => r.DepartmentKey, departments, "Instructors.DepartmentKey");
        Links(file.Programmes, r => r.DepartmentKey, departments, "Programmes.DepartmentKey");
        Links(file.Courses, r => r.DepartmentKey, departments, "Courses.DepartmentKey");
        Links(file.ProgrammeCourses, r => r.ProgrammeKey, programmes, "ProgrammeCourses.ProgrammeKey");
        Links(file.ProgrammeCourses, r => r.CourseKey, courses, "ProgrammeCourses.CourseKey");
        var identities = new HashSet<(string, string)>();
        foreach (var row in file.Courses ?? [])
            if (row is not null && !identities.Add((Key(row.DepartmentKey), Key(row.CourseCode))))
                errors.Add("Courses: duplicate code within a department. Resolve course identity before importing.");
        foreach (var row in file.Departments ?? [])
            if (row?.DepartmentCode is { } code && new[] { "UNKNOWN", "NOT PUBLICLY PUBLISHED", "NULL", "-" }.Contains(Key(code)))
                errors.Add("Departments.DepartmentCode: use JSON null for an unknown official code.");
        return errors;

        HashSet<string> Rows<T>(List<T?>? rows, Func<T, string?> key, string path) where T : class
        {
            var keys = new HashSet<string>(StringComparer.Ordinal);
            if (rows is null) { errors.Add(path + ": list is required; use [] when empty."); return keys; }
            for (var i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                if (row is null) { errors.Add($"{path}[{i}]: null row."); continue; }
                var results = new List<ValidationResult>();
                Validator.TryValidateObject(row, new ValidationContext(row), results, true);
                foreach (var result in results) errors.Add($"{path}[{i}]: {result.ErrorMessage}");
                var normalized = Key(key(row));
                if (normalized.Length == 0 || normalized.Length > 100 || !keys.Add(normalized))
                    errors.Add($"{path}[{i}]: invalid or duplicate stable key.");
            }
            return keys;
        }
        void Links<T>(List<T?>? rows, Func<T, string?> parent, HashSet<string> keys, string path) where T : class
        {
            foreach (var row in rows ?? [])
                if (row is not null && !keys.Contains(Key(parent(row))))
                    errors.Add(path + ": unresolved parent; keep this row in the pending source list.");
        }
    }
    internal static string Key(string? value) => value?.Trim().ToUpperInvariant() ?? "";
    internal static string? Optional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
