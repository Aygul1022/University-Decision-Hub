namespace UniversityDecisionHub.Api.Features.Courses.Dtos
{
    public class CourseSearchParams
    {
        public string? Query { get; set; }
        public int? DepartmentId { get; set; }
        public int? Credits { get; set; }
        public string? CourseType { get; set; }
        public string? Semester { get; set; }
        public int? CareerAreaId { get; set; }
    }
}
