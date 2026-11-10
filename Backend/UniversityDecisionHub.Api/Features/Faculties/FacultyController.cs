using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniversityDecisionHub.Api.Common;
using UniversityDecisionHub.Api.Data;
using UniversityDecisionHub.Api.Features.Departments;
using UniversityDecisionHub.Api.Features.Instructors;
namespace UniversityDecisionHub.Api.Features.Faculties;
[ApiController, Route("api/faculties")]
public class FacultyController(ApplicationDbContext db) : CatalogControllerBase(db)
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct) => Ok(await Db.Faculties.AsNoTracking()
        .OrderBy(f => f.FacultyId).Select(f => new { f.FacultyId, f.FacultyName }).ToListAsync(ct));
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var row = await Db.Faculties.AsNoTracking().Where(f => f.FacultyId == id)
            .Select(f => new { f.FacultyId, f.FacultyName }).SingleOrDefaultAsync(ct);
        return row is null ? NotFound() : Ok(row);
    }
    [HttpPost, CatalogAdministrator]
    public async Task<IActionResult> Post(FacultyCreate input, CancellationToken ct)
    {
        if (input.Departments is null || input.Departments.Count == 0 || input.Departments.Any(d => d is null || d.Instructors is null || d.Instructors.Count == 0 || d.Instructors.Any(i => i is null)))
            return BadRequest("Provide at least one department and at least one instructor per department.");
        var row = new Faculty { FacultyName = input.FacultyName.Trim(), Departments = input.Departments.Select(ToDepartment).ToList() };
        Db.Faculties.Add(row);
        return await Save(() => CreatedAtAction(nameof(GetById), new { id = row.FacultyId }, row), ct);
    }
    internal static Department ToDepartment(DepartmentSeed d) => new()
    {
        DepartmentCode = d.DepartmentCode.Trim(), DepartmentName = d.DepartmentName.Trim(),
        Instructors = d.Instructors.Select(i => new Instructor { FullName = i.FullName.Trim(), AcademicTitle = i.AcademicTitle?.Trim() }).ToList()
    };
    [HttpPut("{id:int}"), CatalogAdministrator]
    public async Task<IActionResult> Put(int id, FacultyInput input, CancellationToken ct)
    {
        var row = await Db.Faculties.FindAsync([id], ct);
        if (row is null) return NotFound();
        row.FacultyName = input.FacultyName.Trim();
        return await Save(() => NoContent(), ct);
    }
    [HttpDelete("{id:int}"), CatalogAdministrator]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var row = await Db.Faculties.FindAsync([id], ct);
        if (row is null) return NotFound();
        // Departments are never silently deleted with a faculty.
        Db.Faculties.Remove(row);
        return await Save(() => NoContent(), ct);
    }
}
