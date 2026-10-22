using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

// <summary-detailed>
// This class represents a Data Transfer Object (DTO) for updating Course information in the University Decision Hub API.
// It contains properties for Course identification, details, and prerequisites.
// The properties are decorated with data annotations to enforce validation rules, such as required fields and string length constraints.
// The PrerequisiteCourseIds property is an optional list that allows specifying prerequisite courses by their IDs.
// </summary-detailed>

namespace UniversityDecisionHub.Api.Features.Courses.Dtos
{
    public class CourseUpdateDto
    {
        [Range(1, int.MaxValue)]
        public int CourseId { get; set; }

        [Required, StringLength(20)]
        public string CourseCode { get; set; } = string.Empty;

        [Required, StringLength(200)]
        public string CourseName { get; set; } = string.Empty;

        public string? CourseDescription { get; set; }

        [Range(0, 30)]
        public int CourseCredit { get; set; }

        [Required, Range(1, int.MaxValue)]
        public int? RecommendedStudyYear { get; set; }

        [Range(0, int.MaxValue)]
        public int Ects { get; set; }
        public bool IsActive { get; set; }
        public bool IsElective { get; set; }

        [Range(1, int.MaxValue)]
        public int DepartmentId { get; set; }

        // Optional list of prerequisite ids
        public List<int>? PrerequisiteCourseIds { get; set; }
    }
}
