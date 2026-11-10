using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using UniversityDecisionHub.Api.Features.Users;

namespace UniversityDecisionHub.Api.Features.Authentication
{
    // ---- Shared contract for the other modules -------------------------------------------
    //   Any logged-in user:   [Authorize]
    //   Administrators only:  [Authorize(Policy = AuthPolicies.Administrator)]
    //   Who is calling:       int? userId = User.GetUserId();
    // ---------------------------------------------------------------------------------------
    public static class AuthPolicies
    {
        public const string Administrator = "Administrator";
    }

    public static class AuthRoles
    {
        public const string Administrator = "Administrator";
    }

    public static class AuthClaims
    {
        public const string UserId = "sub";
        public const string Role = "role";
    }

    public static class RateLimitPolicies
    {
        public const string Authentication = "authentication";
    }

    public static class ClaimsPrincipalExtensions
    {
        // UserAccount.UserId of the authenticated caller, or null for an anonymous request.
        public static int? GetUserId(this ClaimsPrincipal principal) =>
            int.TryParse(principal.FindFirstValue(AuthClaims.UserId), out var id) ? id : null;
    }

    public static class AuthenticationSetup
    {
        public static IServiceCollection AddUdhAuthentication(
            this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
        {
            var jwt = configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();

            if (string.IsNullOrWhiteSpace(jwt.Key))
            {
                if (!environment.IsDevelopment())
                    throw new InvalidOperationException("Configuration value 'Jwt:Key' is required outside Development.");

                // Local convenience only: logins stop being valid when the API restarts.
                jwt.Key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
                Console.WriteLine("warn: Jwt:Key is not configured. Using a temporary signing key for this run; " +
                                  "logins are lost on restart. Set a 32+ character value with: dotnet user-secrets set \"Jwt:Key\" \"...\"");
            }
            if (Encoding.UTF8.GetByteCount(jwt.Key) < 32)
                throw new InvalidOperationException("'Jwt:Key' must be at least 32 characters long.");

            services.AddSingleton(jwt);
            services.AddSingleton<JwtTokenService>();
            services.AddSingleton<IPasswordHasher<UserAccount>, PasswordHasher<UserAccount>>();

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.MapInboundClaims = false;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = jwt.Issuer,
                        ValidateAudience = true,
                        ValidAudience = jwt.Audience,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = jwt.SigningKey(),
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.FromMinutes(1),
                        NameClaimType = AuthClaims.UserId,
                        RoleClaimType = AuthClaims.Role
                    };
                });

            services.AddAuthorization(options =>
                options.AddPolicy(AuthPolicies.Administrator, policy => policy.RequireRole(AuthRoles.Administrator)));

            // Slows down password guessing: 10 register/login attempts per minute per client address.
            services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                options.AddPolicy(RateLimitPolicies.Authentication, context =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1) }));
            });

            return services;
        }
    }
}
