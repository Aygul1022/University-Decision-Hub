using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using UniversityDecisionHub.Api.Data;

namespace UniversityDecisionHub.Api.Features.CatalogImport;

public static class CatalogImportCommand
{
    public const string Name = "import-catalog";

    public static async Task<int> RunAsync(string[] args, string connectionString)
    {
        if (args.Length != 3 || args[2] != "--apply")
        {
            Console.Error.WriteLine("Usage: import-catalog <absolute-catalog.json> --apply");
            return 1;
        }
        try
        {
            // The developer CLI intentionally accepts only each teammate's local database.
            var target = new NpgsqlConnectionStringBuilder(connectionString);
            if (target.Host is not ("127.0.0.1" or "localhost" or "::1") || target.Database != "UniversityDecisionHub")
            {
                Console.Error.WriteLine("Import requires a local UniversityDecisionHub connection in your personal settings.");
                return 1;
            }
            using var stream = File.OpenRead(args[1]);
            var file = await JsonSerializer.DeserializeAsync<CatalogFile>(stream,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true, AllowDuplicateProperties = false });
            if (file is null) throw new CatalogImportConflictException("Expected a catalog JSON object.");
            var errors = CatalogFileValidator.Validate(file);
            if (errors.Count > 0) throw new CatalogImportConflictException(string.Join(Environment.NewLine, errors));

            await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseNpgsql(connectionString).Options);
            if (db.Database.HasPendingModelChanges() || (await db.Database.GetPendingMigrationsAsync()).Any())
            {
                Console.Error.WriteLine("Database schema is not ready. Apply the approved migrations before importing.");
                return 2;
            }
            var result = await new CatalogImportService(db).ImportAsync(file);
            Console.WriteLine($"Import committed to local UniversityDecisionHub: {result.Added} added, {result.Skipped} unchanged. No existing records updated or deleted.");
            return 0;
        }
        catch (CatalogImportConflictException ex)
        {
            Console.Error.WriteLine(ex.Message);
            Console.Error.WriteLine("Import rejected. No changes from this import were committed.");
            return 2;
        }
        catch (JsonException ex)
        {
            Console.Error.WriteLine($"Invalid JSON at line {ex.LineNumber + 1}, byte {ex.BytePositionInLine}. No import performed.");
            return 2;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Console.Error.WriteLine("Cannot read catalog file. Use an absolute path and check permissions.");
            return 1;
        }
        catch (Exception ex) when (ex is DbUpdateException or NpgsqlException or TimeoutException)
        {
            // Do not echo connection strings, personal values or database exception details.
            Console.Error.WriteLine("Database operation failed. Check local connection, migrations and concurrent changes. If the connection failed during commit, verify or rerun the same unchanged file; import keys prevent duplicate insertion.");
            return 3;
        }
    }
}
