using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniversityDecisionHub.Api.Data;
using UniversityDecisionHub.Api.Features.Authentication;
using UniversityDecisionHub.Api.Features.Instructors.Dtos;

namespace UniversityDecisionHub.Api.Features.Instructors
{
    // Reading the basic instructor catalog needs no login; reviews stay restricted elsewhere.
    // Creating and updating records is for administrators only. There is deliberately no DELETE:
    // offerings and reviews will reference instructors and must keep their history.
    [ApiController]
    [Route("api/instructors")]
    public class InstructorController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public InstructorController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<InstructorDto>>> Get([FromQuery] InstructorSearchParams search)
        {
            var q = _db.Instructors.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(search.Query))
            {
                var lower = search.Query.Trim().ToLower();
                q = q.Where(i => i.FullName.ToLower().Contains(lower));
            }

            if (search.DepartmentId != null)
                q = q.Where(i => i.DepartmentId == search.DepartmentId.Value);

            var list = await q.OrderBy(i => i.FullName).ThenBy(i => i.InstructorId).ToListAsync();
            return Ok(list.Select(InstructorDto.From));
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<InstructorDto>> GetById(int id)
        {
            var instructor = await _db.Instructors.AsNoTracking().FirstOrDefaultAsync(i => i.InstructorId == id);
            if (instructor == null) return NotFound();

            return Ok(InstructorDto.From(instructor));
        }

        [HttpPost]
        [Authorize(Policy = AuthPolicies.Administrator)]
        public async Task<ActionResult<InstructorDto>> Post([FromBody] InstructorSaveDto dto)
        {
            var instructor = new Instructor();
            Apply(dto, instructor);

            _db.Instructors.Add(instructor);
            await _db.SaveChangesAsync();

            var result = InstructorDto.From(instructor);
            return CreatedAtAction(nameof(GetById), new { id = result.InstructorId }, result);
        }

        [HttpPut("{id:int}")]
        [Authorize(Policy = AuthPolicies.Administrator)]
        public async Task<ActionResult<InstructorDto>> Put(int id, [FromBody] InstructorSaveDto dto)
        {
            var instructor = await _db.Instructors.FirstOrDefaultAsync(i => i.InstructorId == id);
            if (instructor == null) return NotFound();

            Apply(dto, instructor);
            await _db.SaveChangesAsync();

            return Ok(InstructorDto.From(instructor));
        }

        private static void Apply(InstructorSaveDto dto, Instructor instructor)
        {
            instructor.FullName = dto.FullName.Trim();
            instructor.AcademicTitle = string.IsNullOrWhiteSpace(dto.AcademicTitle) ? null : dto.AcademicTitle.Trim();
            instructor.DepartmentId = dto.DepartmentId;
        }
    }
}
