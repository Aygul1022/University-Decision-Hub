using System.ComponentModel.DataAnnotations;

namespace UniversityDecisionHub.Api.Features.Instructors.Dtos
{
    // GET api/instructors?query=yilmaz&departmentId=3   (both optional, combined with AND)
    public class InstructorSearchParams
    {
        [StringLength(150)]
        public string? Query { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "DepartmentId must be a positive number.")]
        public int? DepartmentId { get; set; }
    }
}
