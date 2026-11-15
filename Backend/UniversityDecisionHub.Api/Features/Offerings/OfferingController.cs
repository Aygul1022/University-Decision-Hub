using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniversityDecisionHub.Api.Common;
using UniversityDecisionHub.Api.Data;
namespace UniversityDecisionHub.Api.Features.Offerings;
[ApiController, Route("api/offerings")]
public class OfferingController(ApplicationDbContext db) : CatalogControllerBase(db)
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] int? courseId, CancellationToken ct) => Ok(await Db.CourseOfferings.AsNoTracking()
        .Where(o => courseId == null || o.CourseId == courseId).OrderBy(o => o.OfferingId).ToListAsync(ct));
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var row = await Db.CourseOfferings.AsNoTracking().SingleOrDefaultAsync(o => o.OfferingId == id, ct);
        return row is null ? NotFound() : Ok(row);
    }
    [HttpPost, CatalogAdministrator]
    public async Task<IActionResult> Post(OfferingCreate input, CancellationToken ct)
    {
        if (!await Db.Courses.AnyAsync(c => c.CourseId == input.CourseId, ct)) return BadRequest("Course does not exist.");
        var row = new CourseOffering { CourseId = input.CourseId, AcademicYear = input.AcademicYear.Trim(), Semester = input.Semester.Trim(), Status = input.Status.Trim() };
        Db.CourseOfferings.Add(row);
        return await Save(() => CreatedAtAction(nameof(GetById), new { id = row.OfferingId }, row), ct);
    }
    [HttpPut("{id:int}"), CatalogAdministrator]
    public async Task<IActionResult> Put(int id, OfferingInput input, CancellationToken ct)
    {
        var row = await Db.CourseOfferings.FindAsync([id], ct);
        if (row is null) return NotFound();
        row.AcademicYear = input.AcademicYear.Trim(); row.Semester = input.Semester.Trim(); row.Status = input.Status.Trim();
        return await Save(() => NoContent(), ct);
    }
    [HttpDelete("{id:int}"), CatalogAdministrator]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var row = await Db.CourseOfferings.FindAsync([id], ct);
        if (row is null) return NotFound();
        Db.CourseOfferings.Remove(row);
        return await Save(() => NoContent(), ct);
    }
    [HttpGet("{id:int}/instructors")]
    public async Task<IActionResult> GetInstructors(int id, CancellationToken ct)
    {
        if (!await Db.CourseOfferings.AnyAsync(o => o.OfferingId == id, ct)) return NotFound();
        return Ok(await (from a in Db.OfferingInstructors.AsNoTracking()
            join i in Db.Instructors on a.InstructorId equals i.InstructorId
            where a.OfferingId == id
            orderby i.InstructorId
            select new { a.OfferingId, i.InstructorId, i.FullName, i.AcademicTitle, i.DepartmentId }).ToListAsync(ct));
    }
    [HttpPost("{id:int}/instructors"), CatalogAdministrator]
    public async Task<IActionResult> Assign(int id, AssignmentInput input, CancellationToken ct)
    {
        if (!await Db.CourseOfferings.AnyAsync(o => o.OfferingId == id, ct)) return NotFound();
        if (!await Db.Instructors.AnyAsync(i => i.InstructorId == input.InstructorId, ct)) return BadRequest("Instructor does not exist.");
        var row = new OfferingInstructor { OfferingId = id, InstructorId = input.InstructorId };
        Db.OfferingInstructors.Add(row);
        return await Save(() => CreatedAtAction(nameof(GetInstructors), new { id }, row), ct);
    }
    [HttpDelete("{id:int}/instructors/{instructorId:int}"), CatalogAdministrator]
    public async Task<IActionResult> Unassign(int id, int instructorId, CancellationToken ct)
    {
        var row = await Db.OfferingInstructors.FindAsync([id, instructorId], ct);
        if (row is null) return NotFound();
        Db.OfferingInstructors.Remove(row);
        return await Save(() => NoContent(), ct);
    }
}
