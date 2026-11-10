using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using UniversityDecisionHub.Api.Data;
namespace UniversityDecisionHub.Api.Common;
public abstract class CatalogControllerBase(ApplicationDbContext db) : ControllerBase
{
    protected ApplicationDbContext Db { get; } = db;
    protected async Task<IActionResult> Save(Func<IActionResult> success, CancellationToken ct)
    {
        try { await Db.SaveChangesAsync(ct); return success(); }
        catch (DbUpdateConcurrencyException) { return Conflict("The record changed. Reload and retry."); }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" or "23503" })
        { return Conflict("A value is duplicated, a referenced record is missing, or this record is still in use."); }
    }
}
