using Npgsql;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniversityDecisionHub.Api.Data;
using UniversityDecisionHub.Api.Features.Courses.Dtos;


// TRY TO UNDERSTAND THE CODE: This code defines a CourseController class that handles HTTP requests related to courses in the University Decision Hub API. It provides endpoints for retrieving, creating, updating, and deleting courses. The controller interacts with the ApplicationDbContext to perform database operations and uses Data Transfer Objects (DTOs) to transfer Course data between the client and server. The controller also includes validation and error handling to ensure proper request processing.

namespace UniversityDecisionHub.Api.Features.Courses
{
    [ApiController]
    [Route("api/courses")]
    public class CourseController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public CourseController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<CourseDto>>> Get([FromQuery] CourseSearchParams query)
        {
            if (query.CourseType != null || query.Semester != null || query.CareerAreaId != null)
                return BadRequest("CourseType, Semester and CareerAreaId filters are not implemented yet.");
            var q = _db.Courses.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(query?.Query))
            {
                var lower = query.Query.Trim().ToLower();
                q = q.Where(c => c.CourseName.ToLower().Contains(lower) || c.CourseCode.ToLower().Contains(lower));
            }

            if (query?.DepartmentId != null)
                q = q.Where(c => c.DepartmentId == query.DepartmentId.Value);

            if (query?.Credits != null)
                q = q.Where(c => c.CourseCredit == query.Credits.Value);

            var list = await q
                .Include(c => c.Prerequisites)
                .ToListAsync();

            var dtos = list.Select(c => ToDto(c));
            return Ok(dtos);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<CourseDto>> GetById(int id)
        {
            var course = await _db.Courses
                .Include(c => c.Prerequisites)
                .FirstOrDefaultAsync(c => c.CourseId == id);

            if (course == null) return NotFound();

            return Ok(ToDto(course));
        }

        [HttpPost]
        public async Task<ActionResult<CourseDto>> Post([FromBody] CourseCreateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var validationError = await ValidatePrerequisites(dto.PrerequisiteCourseIds, null);
            if (validationError != null) return BadRequest(validationError);
            var entity = new Course
            {
                CourseCode = dto.CourseCode,
                CourseName = dto.CourseName,
                CourseCredit = dto.CourseCredit,
                Ects = dto.Ects,
                RecommendedStudyYear = dto.RecommendedStudyYear,
                IsActive = dto.IsActive,
                IsElective = dto.IsElective,
                DepartmentId = dto.DepartmentId,
                CourseDescription = dto.CourseDescription
            };

            // Save the course and its existing prerequisite feature atomically.
            foreach (var pid in dto.PrerequisiteCourseIds ?? [])
                entity.Prerequisites.Add(new CoursePrerequisite { Course = entity, PrerequisiteCourseId = pid });
            _db.Courses.Add(entity);
            var saveError = await SaveCourseChanges();
            if (saveError != null) return saveError;

            var result = ToDto(entity);
            return CreatedAtAction(nameof(GetById), new { id = result.CourseId }, result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] CourseUpdateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (id != dto.CourseId) return BadRequest("Id mismatch");

            var entity = await _db.Courses
                .Include(c => c.Prerequisites)
                .FirstOrDefaultAsync(c => c.CourseId == id);

            if (entity == null) return NotFound();

            var validationError = await ValidatePrerequisites(dto.PrerequisiteCourseIds, id);
            if (validationError != null) return BadRequest(validationError);
            entity.CourseCode = dto.CourseCode;
            entity.CourseName = dto.CourseName;
            entity.CourseCredit = dto.CourseCredit;
            entity.Ects = dto.Ects;
            entity.RecommendedStudyYear = dto.RecommendedStudyYear;
            entity.IsActive = dto.IsActive;
            entity.IsElective = dto.IsElective;
            entity.DepartmentId = dto.DepartmentId;
            entity.CourseDescription = dto.CourseDescription;

            // Keep unchanged links so EF does not track two objects with the same key.
            var desired = (dto.PrerequisiteCourseIds ?? []).ToHashSet();
            var existingIds = entity.Prerequisites.Select(p => p.PrerequisiteCourseId).ToHashSet();
            _db.CoursePrerequisites.RemoveRange(entity.Prerequisites.Where(p => !desired.Contains(p.PrerequisiteCourseId)));
            foreach (var pid in desired.Except(existingIds))
                _db.CoursePrerequisites.Add(new CoursePrerequisite { CourseId = id, PrerequisiteCourseId = pid });

            var saveError = await SaveCourseChanges();
            if (saveError != null) return saveError;
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var entity = await _db.Courses.FindAsync(id);
            if (entity == null) return NotFound();

            // Remove join entries referencing this Course (both sides)
            var related = _db.CoursePrerequisites.Where(cp => cp.CourseId == id || cp.PrerequisiteCourseId == id);
            _db.CoursePrerequisites.RemoveRange(related);

            _db.Courses.Remove(entity);
            var saveError = await SaveCourseChanges();
            if (saveError != null) return saveError;

            return NoContent();
        }

        // These checks repair the existing prerequisite API; no student prerequisite checking is added.
        private async Task<string?> ValidatePrerequisites(List<int>? ids, int? courseId)
        {
            if (ids == null || ids.Count == 0) return null;
            if (ids.Any(id => id <= 0)) return "Prerequisite IDs must be positive.";
            if (ids.Count != ids.Distinct().Count()) return "Prerequisite IDs must not repeat.";
            if (courseId.HasValue && ids.Contains(courseId.Value)) return "A course cannot require itself.";
            if (await _db.Courses.CountAsync(c => ids.Contains(c.CourseId)) != ids.Count)
                return "One or more prerequisite courses do not exist.";
            return null;
        }

        private async Task<ActionResult?> SaveCourseChanges()
        {
            try { await _db.SaveChangesAsync(); return null; }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
            { return Conflict("A course code already exists in this department, or a prerequisite link is duplicated."); }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation })
            { return Conflict("A referenced course no longer exists, or the course is still referenced."); }
        }

        // Helpers
        private CourseDto ToDto(Course c)
        {
            return new CourseDto
            {
                CourseId = c.CourseId,
                CourseCode = c.CourseCode,
                CourseName = c.CourseName,
                CourseCredit = c.CourseCredit,
                Ects = c.Ects,
                RecommendedStudyYear = c.RecommendedStudyYear,
                IsActive = c.IsActive,
                IsElective = c.IsElective,
                DepartmentId = c.DepartmentId,
                PrerequisiteCourseIds = c.Prerequisites?.Select(p => p.PrerequisiteCourseId) ?? new List<int>(),
                CourseDescription = c.CourseDescription
            };
        }
    }
}
