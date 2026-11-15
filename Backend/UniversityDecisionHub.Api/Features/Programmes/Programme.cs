namespace UniversityDecisionHub.Api.Features.Programmes;

// A language/degree-specific programme belongs to an academic department.
public class Programme
{
    public int ProgrammeId { get; set; }
    public string ProgrammeKey { get; set; } = string.Empty;
    public int DepartmentId { get; set; }
    public string ProgrammeName { get; set; } = string.Empty;
    public string? DegreeLevel { get; set; }
    public string? InstructionLanguage { get; set; }
    public string? BolognaProgrammeId { get; set; }
}
