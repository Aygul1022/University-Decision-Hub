namespace UniversityDecisionHub.Api.Features.Faculties;
public class Faculty
{
    public int FacultyId { get; set; }
    [System.Text.Json.Serialization.JsonIgnore]
    public string? ImportKey { get; set; }
    public string FacultyName { get; set; } = string.Empty;
    public List<Departments.Department> Departments { get; set; } = [];
}
