using System.ComponentModel.DataAnnotations;
using UniversityDecisionHub.Api.Data.Configurations;

namespace UniversityDecisionHub.Api.Features.Users.Dtos
{
    // Body of POST api/admin/users and PUT api/admin/users/{id}.
    // There is no password and no administrator field: an administrator never sets someone's password,
    // and administrator access is granted only with the `make-admin` command.
    public class ManagedUserSaveDto
    {
        [Required, StringLength(UserAccountConfiguration.FullNameMaxLength, MinimumLength = 2)]
        public string FullName { get; set; } = string.Empty;

        [Required, EmailAddress, StringLength(UserAccountConfiguration.EmailMaxLength)]
        public string Email { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "DepartmentId must be a positive number.")]
        public int DepartmentId { get; set; }

        [Range(1, 6, ErrorMessage = "StudyYear must be between 1 and 6.")]
        public int StudyYear { get; set; }
    }
}
