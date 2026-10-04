using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace EventManagement.Api.Auth;

public class RequireRoleAttribute : Attribute, IAuthorizationFilter
{
    private readonly string _requiredRole;

    public RequireRoleAttribute(string requiredRole)
    {
        _requiredRole = requiredRole;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        string? role = context.HttpContext.Session.GetString("Role");

        if (string.IsNullOrEmpty(role))
        {
            context.Result = new UnauthorizedObjectResult("You must be logged in.");
            return;
        }

        if (role != _requiredRole)
        {
            context.Result = new ObjectResult("Access denied for your role.")
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }
    }
}
