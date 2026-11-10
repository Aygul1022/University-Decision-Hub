namespace UniversityDecisionHub.Api.Features.Departments;
public class Department
{
    public int DepartmentId { get; set; }
    public int FacultyId { get; set; }
    public string DepartmentCode { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public List<Instructors.Instructor> Instructors { get; set; } = [];
}
