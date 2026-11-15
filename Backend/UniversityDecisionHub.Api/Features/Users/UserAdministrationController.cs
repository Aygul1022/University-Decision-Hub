using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniversityDecisionHub.Api.Data;
using UniversityDecisionHub.Api.Features.Authentication;
using UniversityDecisionHub.Api.Features.Users.Dtos;

namespace UniversityDecisionHub.Api.Features.Users
{
    // Administrator-only user management: see who is on the list, add a person, correct their details.
    // A person added here has no password; they complete registration themselves (POST api/auth/register).
    [ApiController]
    [Route("api/admin/users")]
    [Authorize(Policy = AuthPolicies.Administrator)]
    public class UserAdministrationController : ControllerBase
    {
        private const string EmailTaken = "A user with this email already exists.";

        private readonly ApplicationDbContext _db;

        public UserAdministrationController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ManagedUserDto>>> Get()
        {
            var users = await _db.UserAccounts.AsNoTracking().OrderBy(u => u.FullName).ThenBy(u => u.UserId).ToListAsync();
            return Ok(users.Select(ManagedUserDto.From));
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<ManagedUserDto>> GetById(int id)
        {
            var user = await _db.UserAccounts.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == id);
            if (user == null) return NotFound();

            return Ok(ManagedUserDto.From(user));
        }

        [HttpPost]
        public async Task<ActionResult<ManagedUserDto>> Post([FromBody] ManagedUserSaveDto dto)
        {
            if (!await DepartmentExists(dto.DepartmentId)) return UnknownDepartment();

            var normalizedEmail = UserAccount.NormalizeEmail(dto.Email);
            if (await _db.UserAccounts.AnyAsync(u => u.NormalizedEmail == normalizedEmail))
                return Conflict(EmailTaken);

            var user = new UserAccount
            {
                FullName = dto.FullName.Trim(),
                Email = dto.Email.Trim(),
                NormalizedEmail = normalizedEmail,
                PasswordHash = null,
                DepartmentId = dto.DepartmentId,
                StudyYear = dto.StudyYear,
                IsAdministrator = false,
                CreatedAtUtc = DateTime.UtcNow
            };
            _db.UserAccounts.Add(user);

            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // The same email was added at the same moment; the unique index stopped the second insert.
                _db.Entry(user).State = EntityState.Detached;
                if (await _db.UserAccounts.AnyAsync(u => u.NormalizedEmail == normalizedEmail))
                    return Conflict(EmailTaken);
                throw;
            }

            var result = ManagedUserDto.From(user);
            return CreatedAtAction(nameof(GetById), new { id = result.UserId }, result);
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<ManagedUserDto>> Put(int id, [FromBody] ManagedUserSaveDto dto)
        {
            var user = await _db.UserAccounts.FirstOrDefaultAsync(u => u.UserId == id);
            if (user == null) return NotFound();
            if (!await DepartmentExists(dto.DepartmentId)) return UnknownDepartment();

            var normalizedEmail = UserAccount.NormalizeEmail(dto.Email);
            if (normalizedEmail != user.NormalizedEmail
                && await _db.UserAccounts.AnyAsync(u => u.NormalizedEmail == normalizedEmail))
                return Conflict(EmailTaken);

            user.FullName = dto.FullName.Trim();
            user.Email = dto.Email.Trim();
            user.NormalizedEmail = normalizedEmail;
            user.DepartmentId = dto.DepartmentId;
            user.StudyYear = dto.StudyYear;

            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                await _db.Entry(user).ReloadAsync();
                if (await _db.UserAccounts.AnyAsync(u => u.NormalizedEmail == normalizedEmail && u.UserId != id))
                    return Conflict(EmailTaken);
                throw;
            }

            return Ok(ManagedUserDto.From(user));
        }

        // DepartmentId is a foreign key; an unknown one is answered with 400 instead of a database error.
        private async Task<bool> DepartmentExists(int? departmentId) =>
            departmentId == null || await _db.Departments.AnyAsync(d => d.DepartmentId == departmentId);

        private ActionResult UnknownDepartment()
        {
            ModelState.AddModelError(nameof(ManagedUserSaveDto.DepartmentId), "This department does not exist.");
            return ValidationProblem(ModelState);
        }
    }
}
