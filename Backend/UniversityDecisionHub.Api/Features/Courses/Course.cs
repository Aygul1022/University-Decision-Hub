using System.Collections.Generic;

namespace UniversityDecisionHub.Api.Features.Courses
{
    // NOTE: kept class name `Course` as requested. Properties use PascalCase to follow .NET conventions.
    public class Course
    {
        public int CourseId { get; set; }
        public int DepartmentId { get; set; }
        // Null means a pre-existing course has not yet been assigned a year.
        public int? RecommendedStudyYear { get; set; }

        public string CourseName { get; set; } = string.Empty;
        public string CourseCode { get; set; } = string.Empty;

        public string? CourseDescription { get; set; }

        public int CourseCredit { get; set; }

        public int Ects { get; set; }
        public bool IsActive { get; set; }
        public bool IsElective { get; set; }

        // Navigation for prerequisites (self-referencing many-to-many via CoursePrerequisite)
        public ICollection<CoursePrerequisite> Prerequisites { get; set; } = new List<CoursePrerequisite>();
        // Courses that require this Course
        public ICollection<CoursePrerequisite> RequiredBy { get; set; } = new List<CoursePrerequisite>();
    }
}
