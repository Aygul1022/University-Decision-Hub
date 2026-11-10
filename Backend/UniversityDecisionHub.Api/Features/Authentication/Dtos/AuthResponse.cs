namespace UniversityDecisionHub.Api.Features.Authentication.Dtos
{
    public class AuthResponse
    {
        // Send on later requests as:  Authorization: Bearer <Token>
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresAtUtc { get; set; }
        public UserProfileDto User { get; set; } = null!;
    }
}
