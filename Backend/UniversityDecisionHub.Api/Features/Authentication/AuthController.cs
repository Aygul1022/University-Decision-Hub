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
        private const string EmailTaken = "An account with this email already exists.";

        private readonly ApplicationDbContext _db;
        private readonly IPasswordHasher<UserAccount> _hasher;
        private readonly JwtTokenService _tokens;

        public AuthController(ApplicationDbContext db, IPasswordHasher<UserAccount> hasher, JwtTokenService tokens)
        {
            _db = db;
            _hasher = hasher;
            _tokens = tokens;
        }

        // POST api/auth/register  -> 201 + token, 400 invalid input, 409 email already registered
        [HttpPost("register")]
        [EnableRateLimiting(RateLimitPolicies.Authentication)]
        public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
        {
            var normalizedEmail = UserAccount.NormalizeEmail(request.Email);
            if (await _db.UserAccounts.AnyAsync(u => u.NormalizedEmail == normalizedEmail))
                return Conflict(EmailTaken);

            var user = new UserAccount
            {
                FullName = request.FullName.Trim(),
                Email = request.Email.Trim(),
                NormalizedEmail = normalizedEmail,
                DepartmentId = request.DepartmentId,
                StudyYear = request.StudyYear,
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
                // Two registrations for the same email raced past the check above; the unique index stopped the second.
                _db.Entry(user).State = EntityState.Detached;
                if (await _db.UserAccounts.AnyAsync(u => u.NormalizedEmail == normalizedEmail))
                    return Conflict(EmailTaken);
                throw;
            }

            return StatusCode(StatusCodes.Status201Created, ToResponse(user));
        }

        // POST api/auth/login  -> 200 + token, 401 for any wrong email/password combination
        [HttpPost("login")]
        [EnableRateLimiting(RateLimitPolicies.Authentication)]
        public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
        {
            var normalizedEmail = UserAccount.NormalizeEmail(request.Email);
            var user = await _db.UserAccounts.FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail);

            if (user == null)
            {
                // Do the same hashing work for unknown emails so response time does not reveal which emails exist.
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

        private AuthResponse ToResponse(UserAccount user)
        {
            var (token, expiresAtUtc) = _tokens.CreateToken(user);
            return new AuthResponse { Token = token, ExpiresAtUtc = expiresAtUtc, User = UserProfileDto.From(user) };
        }
    }
}
