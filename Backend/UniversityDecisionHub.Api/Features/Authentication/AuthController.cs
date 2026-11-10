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
        private const string AlreadyRegistered = "This account is already registered. Log in instead.";

        // One message for "email not on the list" and "a detail is different", so the answer does not
        // reveal which emails are on the list or which field was wrong.
        private const string NoMatch = "These details do not match a pre-registered user. " +
            "Check your name, email, department and year, or contact an administrator.";

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
        // Completes the registration of an account that was created in advance: the name, email,
        // department and year must match the stored record, and the person chooses a password.
        //   201 + token   registration completed
        //   400           invalid input
        //   404           no pre-registered user matches these details
        //   409           the account is already registered
        [HttpPost("register")]
        [EnableRateLimiting(RateLimitPolicies.Authentication)]
        public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
        {
            var normalizedEmail = UserAccount.NormalizeEmail(request.Email);
            var user = await _db.UserAccounts.AsNoTracking().FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail);

            var matches = user != null
                && UserAccount.NormalizeName(user.FullName) == UserAccount.NormalizeName(request.FullName)
                && user.DepartmentId == request.DepartmentId
                && user.StudyYear == request.StudyYear;
            if (user == null || !matches)
                return NotFound(NoMatch);

            if (user.HasCompletedRegistration)
                return Conflict(AlreadyRegistered);

            // Set the password only if nobody has set one in the meantime: when two requests for the
            // same account arrive together, exactly one of them wins and the other gets 409.
            var passwordHash = _hasher.HashPassword(user, request.Password);
            var updated = await _db.UserAccounts
                .Where(u => u.UserId == user.UserId && u.PasswordHash == null)
                .ExecuteUpdateAsync(setters => setters.SetProperty(u => u.PasswordHash, passwordHash));
            if (updated == 0)
                return Conflict(AlreadyRegistered);

            return StatusCode(StatusCodes.Status201Created, ToResponse(user));
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

            var user = await _db.UserAccounts.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == userId.Value);
            if (user == null) return Unauthorized();

            return Ok(UserProfileDto.From(user));
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

            if (request.DepartmentId != null && !await _db.Departments.AnyAsync(d => d.DepartmentId == request.DepartmentId))
            {
                ModelState.AddModelError(nameof(request.DepartmentId), "This department does not exist.");
                return ValidationProblem(ModelState);
            }

            user.FullName = request.FullName.Trim();
            user.DepartmentId = request.DepartmentId;
            user.StudyYear = request.StudyYear;
            await _db.SaveChangesAsync();

            return Ok(UserProfileDto.From(user));
        }

        private AuthResponse ToResponse(UserAccount user)
        {
            var (token, expiresAtUtc) = _tokens.CreateToken(user);
            return new AuthResponse { Token = token, ExpiresAtUtc = expiresAtUtc, User = UserProfileDto.From(user) };
        }
    }
}
