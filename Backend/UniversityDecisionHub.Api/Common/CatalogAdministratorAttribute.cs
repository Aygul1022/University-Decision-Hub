using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
namespace UniversityDecisionHub.Api.Common;
// No authentication scheme exists yet. Fail closed until trusted authentication sets this role.
[AttributeUsage(AttributeTargets.Method)]
public sealed class CatalogAdministratorAttribute : Attribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        if (context.HttpContext.User.Identity?.IsAuthenticated != true)
            context.Result = new UnauthorizedResult();
        else if (!context.HttpContext.User.IsInRole("Administrator"))
            context.Result = new StatusCodeResult(StatusCodes.Status403Forbidden);
    }
}
