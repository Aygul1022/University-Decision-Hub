using Microsoft.EntityFrameworkCore;
using UniversityDecisionHub.Api.Data;
namespace UniversityDecisionHub.Api.Features.Programmes;

public static class ProgrammeProfile
{
    public static async Task<(int? DepartmentId, string? Error)> ResolveAsync(ApplicationDbContext db, int? programmeId, int? departmentId)
    {
        if (programmeId is not null)
        {
            var programme = await db.Programmes.AsNoTracking().SingleOrDefaultAsync(p => p.ProgrammeId == programmeId);
            if (programme is null) return (null, "Programme does not exist.");
            if (departmentId is not null && departmentId != programme.DepartmentId)
                return (null, "Department does not match the selected programme.");
            return (programme.DepartmentId, null);
        }
        if (departmentId is not null && !await db.Departments.AnyAsync(d => d.DepartmentId == departmentId))
            return (null, "Department does not exist.");
        return (departmentId, null);
    }
}
