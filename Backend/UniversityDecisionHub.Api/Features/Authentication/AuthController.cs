using UniversityDecisionHub.Api.Features.Programmes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using UniversityDecisionHub.Api.Data;
using UniversityDecisionHub.Api.Features.Authentication.Dtos;
using UniversityDecisionHub.Api.Features.Users;

namespace UniversityDecisionHub.Api.Features.Authentication
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private const string InvalidCredentials = "Email or password is incorrect.";
        private const string AlreadyRegistered = "An account with this email already exists. Log in instead.";

        // A record added in advance (import or administrator) that has no password yet. Nothing proves that
        // the person registering owns that email, so the record is not handed over.
        private const string AddedInAdvance = "This email was added in advance and cannot be registered yet. " +
            "Contact an administrator.";

        private readonly ApplicationDbContext _db;
        private readonly IPasswordHasher<UserAccount> _hasher;
        private readonly JwtTokenService _tokens;

        public AuthController(ApplicationDbContext db, IPasswordHasher<UserAccount> hasher, JwtTokenService tokens)
        {
            _db = db;
            _hasher = hasher;
            _tokens = tokens;
        }

        // POST api/auth/register
        // Open registration: anyone can create an account with an email that has no account yet.
        //   201 + token   account created and logged in
        //   400           invalid input, or an unknown department or programme
        //   403           the email belongs to a record added in advance that has no password yet
        //   409           an account with this email already exists
        // Completing a record added in advance stays closed until the team decides how to verify the email
        // owner. The record is left exactly as it is: name, department, programme and year are not touched.
        [HttpPost("register")]
        [EnableRateLimiting(RateLimitPolicies.Authentication)]
        public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
        {
            if (UserAccount.GraduateError(request.IsGraduate, request.StudyYear) is { } graduateError)
                return BadRequest(graduateError);

            var profile = await ProgrammeProfile.ResolveAsync(_db, request.ProgrammeId, request.DepartmentId);
            if (profile.Error is not null) return BadRequest(profile.Error);

            var normalizedEmail = UserAccount.NormalizeEmail(request.Email);
            var user = await _db.UserAccounts.AsNoTracking().FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail);
            if (user is { HasCompletedRegistration: true })
                return Conflict(AlreadyRegistered);

            var fullName = request.FullName.Trim();
            var email = request.Email.Trim();

            if (user is null)
            {
                // IsAdministrator is never taken from the request; see RegisterRequest.
                user = new UserAccount
                {
                    FullName = fullName,
                    Email = email,
                    NormalizedEmail = normalizedEmail,
                    DepartmentId = profile.DepartmentId,
                    ProgrammeId = request.ProgrammeId,
                    StudyYear = request.StudyYear,
                    IsGraduate = request.IsGraduate,
                    IsAdministrator = false,
                    CreatedAtUtc = DateTime.UtcNow
                };
                user.PasswordHash = _hasher.HashPassword(user, request.Password);
                _db.UserAccounts.Add(user);

                try
                {
                    await _db.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    // The same email was registered at the same moment; the unique index stopped this insert.
                    _db.Entry(user).State = EntityState.Detached;
                    if (await _db.UserAccounts.AnyAsync(u => u.NormalizedEmail == normalizedEmail))
                        return Conflict(AlreadyRegistered);
                    throw;
                }

                return StatusCode(StatusCodes.Status201Created, ToResponse(user));
            }

            return StatusCode(StatusCodes.Status403Forbidden, AddedInAdvance);
        }

        // POST api/auth/login  -> 200 + token, 401 for any wrong email/password combination
        [HttpPost("login")]
        [EnableRateLimiting(RateLimitPolicies.Authentication)]
        public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
        {
            var normalizedEmail = UserAccount.NormalizeEmail(request.Email);
            var user = await _db.UserAccounts.FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail);

            if (user?.PasswordHash == null)
            {
                // Unknown email, or an account whose owner has not completed registration yet.
                // Do the same hashing work so response time does not reveal which emails exist.
                _hasher.HashPassword(new UserAccount(), request.Password);
                return Unauthorized(InvalidCredentials);
            }

            var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
            if (result == PasswordVerificationResult.Failed)
                return Unauthorized(InvalidCredentials);

            if (result == PasswordVerificationResult.SuccessRehashNeeded)
            {
                user.PasswordHash = _hasher.HashPassword(user, request.Password);
                await _db.SaveChangesAsync();
            }

            return Ok(ToResponse(user));
        }

        // GET api/auth/me  -> the logged-in user's current profile, read from the database
        [HttpGet("me")]
        [Authorize]
        public async Task<ActionResult<UserProfileDto>> Me()
        {
            var userId = User.GetUserId();
            if (userId == null) return Unauthorized();

            var profile = await ReadProfileAsync(userId.Value);
            return profile is null ? Unauthorized() : Ok(profile);
        }

        // PUT api/auth/me  -> the logged-in user updates their own name, department and year.
        // Department and year may be left empty; such an account can log in but cannot write reviews.
        // Review eligibility reads these values from the database at submission time, so a change
        // applies to the next submission. Email, password and administrator status are not editable here.
        [HttpPut("me")]
        [Authorize]
        public async Task<ActionResult<UserProfileDto>> UpdateMe([FromBody] ProfileUpdateRequest request)
        {
            var userId = User.GetUserId();
            if (userId == null) return Unauthorized();

            var user = await _db.UserAccounts.FirstOrDefaultAsync(u => u.UserId == userId.Value);
            if (user == null) return Unauthorized();

            if (UserAccount.GraduateError(request.IsGraduate, request.StudyYear) is { } graduateError)
                return BadRequest(graduateError);

            var profile = await ProgrammeProfile.ResolveAsync(_db, request.ProgrammeId, request.DepartmentId);
            if (profile.Error is not null) return BadRequest(profile.Error);

            user.FullName = request.FullName.Trim();
            user.DepartmentId = profile.DepartmentId;
            user.ProgrammeId = request.ProgrammeId;
            user.StudyYear = request.StudyYear;
            user.IsGraduate = request.IsGraduate;
            await _db.SaveChangesAsync();

            var savedProfile = await ReadProfileAsync(user.UserId);
            return savedProfile is null ? Unauthorized() : Ok(savedProfile);
        }

        private async Task<UserProfileDto?> ReadProfileAsync(int userId)
        {
            var row = await _db.UserAccounts.AsNoTracking()
                .Where(user => user.UserId == userId)
                .Select(user => new
                {
                    User = user,
                    DepartmentName = _db.Departments.Where(department => department.DepartmentId == user.DepartmentId)
                        .Select(department => department.DepartmentName).FirstOrDefault(),
                    ProgrammeName = _db.Programmes.Where(programme => programme.ProgrammeId == user.ProgrammeId)
                        .Select(programme => programme.ProgrammeName).FirstOrDefault()
                })
                .SingleOrDefaultAsync();

            return row is null ? null : UserProfileDto.From(row.User, row.DepartmentName, row.ProgrammeName);
        }

        private AuthResponse ToResponse(UserAccount user)
        {
            var (token, expiresAtUtc) = _tokens.CreateToken(user);
            return new AuthResponse { Token = token, ExpiresAtUtc = expiresAtUtc, User = UserProfileDto.From(user) };
        }
    }
}
