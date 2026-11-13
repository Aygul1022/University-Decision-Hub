namespace UniversityDecisionHub.Api.Features.Instructors.Dtos
{
    // One entry of GET api/instructors/{id}/courses: a course and the periods the instructor taught it.
    public class InstructorCourseDto
    {
        public int CourseId { get; set; }
        public string CourseCode { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;
        public List<InstructorCoursePeriodDto> Periods { get; set; } = [];
    }

    public class InstructorCoursePeriodDto
    {
        public int OfferingId { get; set; }
        public string AcademicYear { get; set; } = string.Empty;
        public string Semester { get; set; } = string.Empty;
    }
}
