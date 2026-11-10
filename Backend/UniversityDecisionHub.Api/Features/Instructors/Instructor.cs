namespace UniversityDecisionHub.Api.Features.Instructors
{
    // A catalog record about a person who teaches (table: Instructors).
    // It is NOT a login account and has no link to UserAccount.
    public class Instructor
    {
        public int InstructorId { get; set; }

        public string FullName { get; set; } = string.Empty;

        // Optional, for example "Assoc. Prof. Dr.". Null when unknown.
        public string? AcademicTitle { get; set; }

        // Foreign key to Departments.
        public int DepartmentId { get; set; }
    }
}
