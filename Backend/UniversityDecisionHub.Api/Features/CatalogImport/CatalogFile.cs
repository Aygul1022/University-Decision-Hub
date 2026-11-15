using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
namespace UniversityDecisionHub.Api.Features.CatalogImport;

// Version 2 is self-contained and links rows by stable source keys, never local IDs.
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class CatalogFile
{
    public int SchemaVersion { get; set; }
    public List<FacultyRow?>? Faculties { get; set; }
    public List<DepartmentRow?>? Departments { get; set; }
    public List<InstructorRow?>? Instructors { get; set; }
    public List<ProgrammeRow?>? Programmes { get; set; }
    public List<CourseRow?>? Courses { get; set; }
    public List<CurriculumRow?>? ProgrammeCourses { get; set; }
}
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class FacultyRow
{
    [Required, StringLength(100)] public string? FacultyKey { get; set; }
    [Required, StringLength(200)] public string? FacultyName { get; set; }
}
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class DepartmentRow
{
    [Required, StringLength(100)] public string? DepartmentKey { get; set; }
    [StringLength(30)] public string? DepartmentCode { get; set; }
    [Required, StringLength(200)] public string? DepartmentName { get; set; }
    [Required, StringLength(100)] public string? FacultyKey { get; set; }
}
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class InstructorRow
{
    [Required, StringLength(100)] public string? InstructorKey { get; set; }
    [Required, StringLength(150)] public string? FullName { get; set; }
    [StringLength(50)] public string? AcademicTitle { get; set; }
    [Required, StringLength(100)] public string? DepartmentKey { get; set; }
}
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class ProgrammeRow
{
    [Required, StringLength(100)] public string? ProgrammeKey { get; set; }
    [Required, StringLength(100)] public string? DepartmentKey { get; set; }
    [Required, StringLength(200)] public string? ProgrammeName { get; set; }
    [StringLength(100)] public string? DegreeLevel { get; set; }
    [StringLength(100)] public string? InstructionLanguage { get; set; }
    [StringLength(100)] public string? BolognaProgrammeId { get; set; }
}
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class CourseRow
{
    [Required, StringLength(100)] public string? CourseKey { get; set; }
    [Required, StringLength(100)] public string? DepartmentKey { get; set; }
    [Required, StringLength(20)] public string? CourseCode { get; set; }
    [Required, StringLength(200)] public string? CourseName { get; set; }
    public string? CourseDescription { get; set; }
}
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class CurriculumRow
{
    [Required, StringLength(100)] public string? CurriculumKey { get; set; }
    [Required, StringLength(100)] public string? ProgrammeKey { get; set; }
    [Required, StringLength(100)] public string? CourseKey { get; set; }
    [StringLength(50)] public string? CatalogueYear { get; set; }
    [Range(1, int.MaxValue)] public int? Semester { get; set; }
    [Range(1, int.MaxValue)] public int? RecommendedStudyYear { get; set; }
    public bool? IsElective { get; set; }
    [Range(typeof(decimal), "0", "79228162514264337593543950335")] public decimal? CourseCredit { get; set; }
    [Range(typeof(decimal), "0", "79228162514264337593543950335")] public decimal? Ects { get; set; }
}
