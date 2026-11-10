using System.ComponentModel.DataAnnotations;
namespace UniversityDecisionHub.Api.Features.Departments;
public class InstructorInput
{
    [Required, StringLength(200)] public string FullName { get; set; } = string.Empty;
    [StringLength(100)] public string? AcademicTitle { get; set; }
}
public class DepartmentInput
{
    [Required, StringLength(30)] public string DepartmentCode { get; set; } = string.Empty;
    [Required, StringLength(200)] public string DepartmentName { get; set; } = string.Empty;
}
public class DepartmentSeed : DepartmentInput
{
    [Required, MinLength(1)] public List<InstructorInput> Instructors { get; set; } = [];
}
public class DepartmentCreate : DepartmentSeed
{
    [Range(1, int.MaxValue)] public int FacultyId { get; set; }
}
public class FacultyInput
{
    [Required, StringLength(200)] public string FacultyName { get; set; } = string.Empty;
}
public class FacultyCreate : FacultyInput
{
    [Required, MinLength(1)] public List<DepartmentSeed> Departments { get; set; } = [];
}
