namespace UniversityDecisionHub.Api.Features.Users;
// Shared profile foundation. Authentication/password storage belongs to the account module.
public class UserAccount
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int? DepartmentId { get; set; }
    public int? StudyYear { get; set; }
    public bool IsAdministrator { get; set; }
}
