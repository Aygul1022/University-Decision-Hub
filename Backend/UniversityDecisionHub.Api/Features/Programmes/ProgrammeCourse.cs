namespace UniversityDecisionHub.Api.Features.Programmes;

// Preserve separate curriculum occurrences. Unknown source values stay null.
public class ProgrammeCourse
{
    public int ProgrammeCourseId { get; set; }
    public string? CurriculumKey { get; set; }
    public int ProgrammeId { get; set; }
    public int CourseId { get; set; }
    public string? CatalogueYear { get; set; }
    public int? Semester { get; set; }
    public int? RecommendedStudyYear { get; set; }
    public bool? IsElective { get; set; }
    public decimal? CourseCredit { get; set; }
    public decimal? Ects { get; set; }
}
