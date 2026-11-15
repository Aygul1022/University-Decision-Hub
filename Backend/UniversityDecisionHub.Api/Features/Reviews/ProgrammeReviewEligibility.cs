using UniversityDecisionHub.Api.Features.Programmes;

namespace UniversityDecisionHub.Api.Features.Reviews;

// Submission uses every curriculum occurrence in the student programme, regardless of offering status.
public static class ProgrammeReviewEligibility
{
    public static string? Check(int? programmeId, int? studyYear, int courseId,
        IEnumerable<ProgrammeCourse> curriculum)
    {
        if (programmeId is null or <= 0 || studyYear is null or <= 0)
            return "Complete your programme and study year before reviewing.";
        var entries = curriculum.Where(p => p.ProgrammeId == programmeId && p.CourseId == courseId).ToArray();
        if (entries.Length == 0)
            return "This course is not in your programme's curricula.";
        if (entries.Any(p => p.RecommendedStudyYear is <= 0))
            return "The curriculum contains an invalid recommended study year.";
        var years = entries.Where(p => p.RecommendedStudyYear.HasValue)
            .Select(p => p.RecommendedStudyYear!.Value).Distinct().ToArray();
        // Do not silently choose a minimum/maximum until the team agrees that policy.
        if (years.Length > 1)
            return "Conflicting curriculum study years require clarification.";
        if (years.Length == 1 && studyYear < years[0])
            return "Your study year does not meet this course's recommended year.";
        return null;
    }
}
