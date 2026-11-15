using System.ComponentModel.DataAnnotations;

namespace UniversityDecisionHub.Api.Features.Authentication.Dtos
{
    public class LoginRequest
    {
        [Required, StringLength(254)]
        public string Email { get; set; } = string.Empty;

        [Required, StringLength(128)]
        public string Password { get; set; } = string.Empty;
    }
}
