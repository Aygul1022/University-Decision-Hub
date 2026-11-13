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

        // GET api/instructors/{id}/courses
        // The courses this instructor is assigned to, each with the periods they taught it.
        // Reads the shared Course, CourseOffering and OfferingInstructor records; nothing is stored here.
        [HttpGet("{id:int}/courses")]
        public async Task<ActionResult<IEnumerable<InstructorCourseDto>>> GetCourses(int id)
        {
            if (!await _db.Instructors.AnyAsync(i => i.InstructorId == id)) return NotFound();

            var rows = await (
                from assignment in _db.OfferingInstructors.AsNoTracking()
                join offering in _db.CourseOfferings on assignment.OfferingId equals offering.OfferingId
                join course in _db.Courses on offering.CourseId equals course.CourseId
                where assignment.InstructorId == id
                select new
                {
                    course.CourseId, course.CourseCode, course.CourseName,
                    offering.OfferingId, offering.AcademicYear, offering.Semester
                }).ToListAsync();

            var courses = rows
                .GroupBy(r => new { r.CourseId, r.CourseCode, r.CourseName })
                .OrderBy(g => g.Key.CourseCode).ThenBy(g => g.Key.CourseId)
                .Select(g => new InstructorCourseDto
                {
                    CourseId = g.Key.CourseId,
                    CourseCode = g.Key.CourseCode,
                    CourseName = g.Key.CourseName,
                    // Newest academic year first.
                    Periods = g.OrderByDescending(r => r.AcademicYear).ThenBy(r => r.Semester).ThenBy(r => r.OfferingId)
                        .Select(r => new InstructorCoursePeriodDto
                        {
                            OfferingId = r.OfferingId, AcademicYear = r.AcademicYear, Semester = r.Semester
                        }).ToList()
                });
            return Ok(courses);
        }

        [HttpPost]
        [Authorize(Policy = AuthPolicies.Administrator)]
        public async Task<ActionResult<InstructorDto>> Post([FromBody] InstructorSaveDto dto)
        {
            if (!await DepartmentExists(dto.DepartmentId)) return UnknownDepartment();

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
            if (!await DepartmentExists(dto.DepartmentId)) return UnknownDepartment();

            Apply(dto, instructor);
            await _db.SaveChangesAsync();

            return Ok(InstructorDto.From(instructor));
        }

        // DepartmentId is a foreign key; an unknown one is answered with 400 instead of a database error.
        private Task<bool> DepartmentExists(int departmentId) =>
            _db.Departments.AnyAsync(d => d.DepartmentId == departmentId);

        private ActionResult UnknownDepartment()
        {
            ModelState.AddModelError(nameof(InstructorSaveDto.DepartmentId), "This department does not exist.");
            return ValidationProblem(ModelState);
        }

        private static void Apply(InstructorSaveDto dto, Instructor instructor)
        {
            instructor.FullName = dto.FullName.Trim();
            instructor.AcademicTitle = string.IsNullOrWhiteSpace(dto.AcademicTitle) ? null : dto.AcademicTitle.Trim();
            instructor.DepartmentId = dto.DepartmentId;
        }
    }
}
