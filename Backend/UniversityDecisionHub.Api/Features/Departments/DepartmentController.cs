using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using UniversityDecisionHub.Api.Common;
using UniversityDecisionHub.Api.Data;
using UniversityDecisionHub.Api.Features.Faculties;
namespace UniversityDecisionHub.Api.Features.Departments;
[ApiController, Route("api/departments")]
public class DepartmentController(ApplicationDbContext db) : CatalogControllerBase(db)
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] int? facultyId, CancellationToken ct) => Ok(await Db.Departments.AsNoTracking()
        .Where(d => facultyId == null || d.FacultyId == facultyId).OrderBy(d => d.DepartmentId)
        .Select(d => new { d.DepartmentId, d.FacultyId, d.DepartmentCode, d.DepartmentName }).ToListAsync(ct));
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var row = await Db.Departments.AsNoTracking().Include(d => d.Instructors).SingleOrDefaultAsync(d => d.DepartmentId == id, ct);
        return row is null ? NotFound() : Ok(row);
    }
    [HttpPost, CatalogAdministrator]
    public async Task<IActionResult> Post(DepartmentCreate input, CancellationToken ct)
    {
        if (input.Instructors is null || input.Instructors.Count == 0 || input.Instructors.Any(i => i is null))
            return BadRequest("Provide at least one instructor.");
        if (!await Db.Faculties.AnyAsync(f => f.FacultyId == input.FacultyId, ct)) return BadRequest("Faculty does not exist.");
        var row = FacultyController.ToDepartment(input); row.FacultyId = input.FacultyId;
        Db.Departments.Add(row);
        return await Save(() => CreatedAtAction(nameof(GetById), new { id = row.DepartmentId }, row), ct);
    }
    [HttpPut("{id:int}"), CatalogAdministrator]
    public async Task<IActionResult> Put(int id, DepartmentInput input, CancellationToken ct)
    {
        var row = await Db.Departments.FindAsync([id], ct);
        if (row is null) return NotFound();
        row.DepartmentCode = input.DepartmentCode.Trim(); row.DepartmentName = input.DepartmentName.Trim();
        return await Save(() => NoContent(), ct);
    }
    [HttpDelete("{id:int}"), CatalogAdministrator]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        try
        {
            await using var tx = await Db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            var row = await Db.Departments.Include(d => d.Instructors).SingleOrDefaultAsync(d => d.DepartmentId == id, ct);
            if (row is null) return NotFound();
            if (await Db.Departments.CountAsync(d => d.FacultyId == row.FacultyId, ct) <= 1)
                return Conflict("A faculty must retain at least one department.");
            // Remove only this department's instructors; assigned instructors and courses block deletion via FKs.
            Db.Instructors.RemoveRange(row.Instructors); Db.Departments.Remove(row);
            var result = await Save(() => NoContent(), ct);
            if (result is NoContentResult) await tx.CommitAsync(ct);
            return result;
        }
        catch (Exception ex) when (ex is PostgresException { SqlState: "40001" } || ex.InnerException is PostgresException { SqlState: "40001" })
        { return Conflict("The catalog changed concurrently. Reload and retry."); }
    }
}
