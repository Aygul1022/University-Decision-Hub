using System.Collections.Generic;

namespace UniversityDecisionHub.Api.Features.Courses.Dtos
{
    public class CourseDto
    {
        public int CourseId { get; set; }
        public string CourseCode { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;

        public string? CourseDescription { get; set; }
        public int CourseCredit { get; set; }
        public int? RecommendedStudyYear { get; set; }

        public int Ects { get; set; }
        public bool IsActive { get; set; }
        public bool IsElective { get; set; }
        public int DepartmentId { get; set; }
        public string? DepartmentName { get; set; }
        public IEnumerable<int> PrerequisiteCourseIds { get; set; } = new List<int>();
    }
}
