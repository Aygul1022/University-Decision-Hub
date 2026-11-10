using System.ComponentModel.DataAnnotations;
using UniversityDecisionHub.Api.Data.Configurations;

namespace UniversityDecisionHub.Api.Features.Instructors.Dtos
{
    // Body of both POST api/instructors and PUT api/instructors/{id}.
    public class InstructorSaveDto
    {
        [Required, StringLength(InstructorConfiguration.FullNameMaxLength, MinimumLength = 2)]
        public string FullName { get; set; } = string.Empty;

        [StringLength(InstructorConfiguration.AcademicTitleMaxLength)]
        public string? AcademicTitle { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "DepartmentId must be a positive number.")]
        public int DepartmentId { get; set; }
    }
}
