using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using UniversityDecisionHub.Api.Features.Users;

namespace UniversityDecisionHub.Api.Features.Authentication
{
    // Bound from the "Jwt" configuration section. The Key comes from User Secrets, never from source control.
    public class JwtOptions
    {
        public string Issuer { get; set; } = "UniversityDecisionHub";
        public string Audience { get; set; } = "UniversityDecisionHub";
        public string Key { get; set; } = string.Empty;
        public int LifetimeMinutes { get; set; } = 120;

        public SymmetricSecurityKey SigningKey() => new(Encoding.UTF8.GetBytes(Key));
    }

    public class JwtTokenService
    {
        private readonly JwtOptions _options;

        public JwtTokenService(JwtOptions options)
        {
            _options = options;
        }

        public (string Token, DateTime ExpiresAtUtc) CreateToken(UserAccount user)
        {
            var claims = new List<Claim> { new(AuthClaims.UserId, user.UserId.ToString()) };
            if (user.IsAdministrator)
                claims.Add(new Claim(AuthClaims.Role, AuthRoles.Administrator));

            var expires = DateTime.UtcNow.AddMinutes(_options.LifetimeMinutes);
            var token = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
            {
                Issuer = _options.Issuer,
                Audience = _options.Audience,
                Subject = new ClaimsIdentity(claims),
                Expires = expires,
                SigningCredentials = new SigningCredentials(_options.SigningKey(), SecurityAlgorithms.HmacSha256)
            });
            return (token, expires);
        }
    }
}
