namespace UniversityDecisionHub.Api.Features.Reviews.Dtos;
// Creation receipt only; Sami owns the separate reading/search DTOs.
public record ReviewCreatedDto(int ReviewId, int UserId, int CourseId, int OfferingId,
    int InstructorId, string ReviewText, DateTime CreatedAt);
