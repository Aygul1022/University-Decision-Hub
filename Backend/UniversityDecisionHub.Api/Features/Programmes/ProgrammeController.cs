using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniversityDecisionHub.Api.Data;
namespace UniversityDecisionHub.Api.Features.Programmes;

[ApiController, Route("api/programmes")]
public class ProgrammeController(ApplicationDbContext db) : ControllerBase
{
    // Public catalogue lookup supports programme selection during registration.
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] int? departmentId, CancellationToken ct)
    {
        if (departmentId is <= 0) return BadRequest("DepartmentId must be positive.");
        return Ok(await db.Programmes.AsNoTracking().Where(p => departmentId == null || p.DepartmentId == departmentId)
            .OrderBy(p => p.ProgrammeName).Select(p => new { p.ProgrammeId, p.DepartmentId, p.ProgrammeName,
                p.DegreeLevel, p.InstructionLanguage }).ToListAsync(ct));
    }

    [HttpGet("{id:int}/courses"), Authorize]
    public async Task<IActionResult> Courses(int id, CancellationToken ct)
    {
        if (!await db.Programmes.AnyAsync(p => p.ProgrammeId == id, ct)) return NotFound();
        // Return all curriculum occurrences, including historical and elective entries.
        return Ok(await (from p in db.ProgrammeCourses.AsNoTracking()
            join c in db.Courses on p.CourseId equals c.CourseId
            where p.ProgrammeId == id
            orderby c.CourseCode, p.ProgrammeCourseId
            select new { p.ProgrammeCourseId, p.CourseId, c.CourseCode, c.CourseName, p.CatalogueYear,
                p.Semester, p.RecommendedStudyYear, p.IsElective, p.CourseCredit, p.Ects }).ToListAsync(ct));
    }
}
