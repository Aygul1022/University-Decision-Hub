using System.Text.Json;

namespace UniversityDecisionHub.Api.Features.CatalogImport;

public static class CatalogValidationCommand
{
    public const string Name = "validate-catalog";

    public static async Task<int> RunAsync(string[] args)
    {
        if (args.Length != 2)
        {
            Console.Error.WriteLine("Usage: validate-catalog <catalog.json>");
            return 1;
        }
        try
        {
            using var stream = File.OpenRead(args[1]);
            var file = await JsonSerializer.DeserializeAsync<CatalogFile>(stream,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true, AllowDuplicateProperties = false });
            if (file is null)
            {
                Console.Error.WriteLine("Invalid catalog: expected a JSON object.");
                return 2;
            }
            var errors = CatalogFileValidator.Validate(file);
            foreach (var error in errors) Console.Error.WriteLine(error);
            if (errors.Count > 0)
            {
                Console.Error.WriteLine($"Validation failed: {errors.Count} issue(s). No database connection or writes.");
                return 2;
            }
            Console.WriteLine($"Validation passed: {file.Faculties!.Count} faculties, {file.Departments!.Count} departments, {file.Instructors!.Count} instructors, {file.Programmes!.Count} programmes, {file.Courses!.Count} courses, {file.ProgrammeCourses!.Count} curriculum rows. No database connection or writes.");
            return 0;
        }
        catch (JsonException ex)
        {
            Console.Error.WriteLine($"Invalid JSON or unsupported field at line {ex.LineNumber + 1}, byte {ex.BytePositionInLine}. Check field names, types and duplicate properties.");
            return 2;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Console.Error.WriteLine("Cannot read catalog file. Check the path and file access permissions.");
            return 1;
        }
    }
}
