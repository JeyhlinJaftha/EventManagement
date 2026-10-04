using EventManagement.Api.Auth;
using EventManagement.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;

namespace EventManagement.Tests;

public class AuthTests
{
    private static AuthorizationFilterContext FilterContext(HttpContext http) =>
        new(new ActionContext(http, new RouteData(), new ActionDescriptor()),
            new List<IFilterMetadata>());

    [Fact]
    public void RequireRole_NoLogin_Returns401()
    {
        var ctx = FilterContext(TestHelpers.CreateHttpContext());
        new RequireRoleAttribute("Organiser").OnAuthorization(ctx);
        Assert.IsType<UnauthorizedObjectResult>(ctx.Result);
    }

    [Fact]
    public void RequireRole_WrongRole_Returns403()
    {
        var ctx = FilterContext(TestHelpers.CreateHttpContext(1, "Participant"));
        new RequireRoleAttribute("Organiser").OnAuthorization(ctx);
        var result = Assert.IsType<ObjectResult>(ctx.Result);
        Assert.Equal(403, result.StatusCode);
    }

    [Fact]
    public void RequireRole_CorrectRole_Allows()
    {
        var ctx = FilterContext(TestHelpers.CreateHttpContext(1, "Organiser"));
        new RequireRoleAttribute("Organiser").OnAuthorization(ctx);
        Assert.Null(ctx.Result);
    }

    [Fact]
    public void RequireLogin_NoLogin_Returns401()
    {
        var ctx = FilterContext(TestHelpers.CreateHttpContext());
        new RequireLoginAttribute().OnAuthorization(ctx);
        Assert.IsType<UnauthorizedObjectResult>(ctx.Result);
    }

    [Fact]
    public void Register_HashesPassword()
    {
        using var db = TestHelpers.CreateDb();
        var controller = new AuthController(db);

        var result = controller.Register(
            new RegisterRequest("Jane", "jane@test.com", "Password123!", "Participant", null));

        Assert.IsType<OkObjectResult>(result);
        var user = db.Users.Single();
        Assert.NotEqual("Password123!", user.PasswordHash);
        Assert.True(BCrypt.Net.BCrypt.Verify("Password123!", user.PasswordHash));
    }

    [Fact]
    public void Register_InvalidRole_ReturnsBadRequest()
    {
        using var db = TestHelpers.CreateDb();
        var controller = new AuthController(db);

        var result = controller.Register(
            new RegisterRequest("Bad", "bad@test.com", "Password123!", "Admin", null));

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public void Register_DuplicateEmail_ReturnsBadRequest()
    {
        using var db = TestHelpers.CreateDb();
        var controller = new AuthController(db);
        controller.Register(new RegisterRequest("A", "a@test.com", "Password123!", "Organiser", null));

        var result = controller.Register(
            new RegisterRequest("B", "a@test.com", "Password123!", "Organiser", null));

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public void Login_ValidCredentials_SetsSession()
    {
        using var db = TestHelpers.CreateDb();
        var controller = new AuthController(db)
        {
            ControllerContext = new ControllerContext { HttpContext = TestHelpers.CreateHttpContext() }
        };
        controller.Register(new RegisterRequest("Org", "org@test.com", "Password123!", "Organiser", null));

        var result = controller.Login(new LoginRequest("org@test.com", "Password123!"));

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal("Organiser", controller.HttpContext.Session.GetString("Role"));
        Assert.NotNull(controller.HttpContext.Session.GetInt32("UserId"));
    }

    [Fact]
    public void Login_WrongPassword_Returns401()
    {
        using var db = TestHelpers.CreateDb();
        var controller = new AuthController(db)
        {
            ControllerContext = new ControllerContext { HttpContext = TestHelpers.CreateHttpContext() }
        };
        controller.Register(new RegisterRequest("Org", "org@test.com", "Password123!", "Organiser", null));

        var result = controller.Login(new LoginRequest("org@test.com", "wrong"));

        Assert.IsType<UnauthorizedObjectResult>(result);
    }
}
