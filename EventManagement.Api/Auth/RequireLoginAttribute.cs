using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace EventManagement.Api.Auth;

public class RequireLoginAttribute : Attribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        int? userId = context.HttpContext.Session.GetInt32("UserId");

        if (userId == null)
        {
            context.Result = new UnauthorizedObjectResult("You must be logged in.");
        }
    }
}
