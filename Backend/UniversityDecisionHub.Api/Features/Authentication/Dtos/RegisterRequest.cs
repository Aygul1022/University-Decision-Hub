using System.ComponentModel.DataAnnotations;
using UniversityDecisionHub.Api.Data.Configurations;

namespace UniversityDecisionHub.Api.Features.Authentication.Dtos
{
    // Deliberately has no IsAdministrator field: public registration can never grant it.
    public class RegisterRequest
    {
        [Required, StringLength(UserAccountConfiguration.FullNameMaxLength, MinimumLength = 2)]
        public string FullName { get; set; } = string.Empty;

        [Required, EmailAddress, StringLength(UserAccountConfiguration.EmailMaxLength)]
        public string Email { get; set; } = string.Empty;

        [Required, StringLength(128, MinimumLength = 8, ErrorMessage = "Password must be between 8 and 128 characters.")]
        public string Password { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "DepartmentId must be a positive number.")]
        public int DepartmentId { get; set; }

        [Range(1, 6, ErrorMessage = "StudyYear must be between 1 and 6.")]
        public int StudyYear { get; set; }
    }
}
