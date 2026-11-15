using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace UniversityDecisionHub.Api.Features.Courses.Dtos
{
    public class CourseCreateDto
    {
        [Required, StringLength(20)]
        public string CourseCode { get; set; } = string.Empty;

        [Required, StringLength(200)]
        public string CourseName { get; set; } = string.Empty;

        public string? CourseDescription { get; set; }

        [Range(typeof(decimal), "0", "30")]
        public decimal? CourseCredit { get; set; }

        [Range(1, int.MaxValue)]
        public int? RecommendedStudyYear { get; set; }

        [Range(typeof(decimal), "0", "79228162514264337593543950335")]
        public decimal? Ects { get; set; }
        public bool? IsActive { get; set; }
        public bool? IsElective { get; set; }

        [Range(1, int.MaxValue)]
        public int DepartmentId { get; set; }

        // Optional list of prerequisite ids
        public List<int>? PrerequisiteCourseIds { get; set; }
    }
}
