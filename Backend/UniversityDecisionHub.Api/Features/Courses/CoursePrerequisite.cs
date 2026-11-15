namespace UniversityDecisionHub.Api.Features.Courses
{
    public class CoursePrerequisite
    {
        public int CourseId { get; set; }
        public Course? Course { get; set; }
        public int PrerequisiteCourseId { get; set; }
        public Course? PrerequisiteCourse { get; set; }
    }
}
