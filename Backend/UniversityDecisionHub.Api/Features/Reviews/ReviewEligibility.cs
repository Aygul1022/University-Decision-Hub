namespace UniversityDecisionHub.Api.Features.Reviews;
public static class ReviewEligibility
{
    public static string? Check(int? studentDepartmentId, int? studyYear, int courseDepartmentId, int? recommendedYear)
    {
        if (studentDepartmentId is null or <= 0 || studyYear is null or <= 0)
            return "Complete your department and study year before reviewing.";
        if (studentDepartmentId != courseDepartmentId)
            return "You can review only courses in your department.";
        if (recommendedYear is null or <= 0)
            return "This course has no valid recommended study year yet.";
        if (studyYear < recommendedYear)
            return "Your study year does not meet this course's recommended year.";
        return null;
    }
}
