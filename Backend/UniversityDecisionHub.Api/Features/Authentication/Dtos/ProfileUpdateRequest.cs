using System.ComponentModel.DataAnnotations;
using UniversityDecisionHub.Api.Data.Configurations;

namespace UniversityDecisionHub.Api.Features.Authentication.Dtos
{
    // Body of PUT api/auth/me. Email, password and administrator status cannot be changed here.
    public class ProfileUpdateRequest
    {
        [Required, StringLength(UserAccountConfiguration.FullNameMaxLength, MinimumLength = 2)]
        public string FullName { get; set; } = string.Empty;

        // Optional; when given it must be an existing department.
        [Range(1, int.MaxValue, ErrorMessage = "DepartmentId must be a positive number.")]
        public int? DepartmentId { get; set; }

        // Optional; when given it must be 1-6.
        [Range(1, 6, ErrorMessage = "StudyYear must be between 1 and 6.")]
        public int? StudyYear { get; set; }
    }
}
