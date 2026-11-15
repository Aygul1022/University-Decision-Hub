namespace UniversityDecisionHub.Api.Features.Departments;
public class Department
{
    public int DepartmentId { get; set; }
    public int FacultyId { get; set; }
    // Stable source identity; null for legacy records until explicitly reconciled.
    public string? DepartmentKey { get; set; }
    public string? DepartmentCode { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public List<Instructors.Instructor> Instructors { get; set; } = [];
}
