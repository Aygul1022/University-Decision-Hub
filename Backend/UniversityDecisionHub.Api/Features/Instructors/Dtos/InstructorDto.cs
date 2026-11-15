namespace UniversityDecisionHub.Api.Features.Instructors.Dtos
{
    public class InstructorDto
    {
        public int InstructorId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? AcademicTitle { get; set; }
        public int DepartmentId { get; set; }

        public static InstructorDto From(Instructor instructor) => new()
        {
            InstructorId = instructor.InstructorId,
            FullName = instructor.FullName,
            AcademicTitle = instructor.AcademicTitle,
            DepartmentId = instructor.DepartmentId
        };
    }
}
