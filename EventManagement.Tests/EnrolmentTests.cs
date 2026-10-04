using EventManagement.Api.Controllers;
using EventManagement.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace EventManagement.Tests;

public class EnrolmentTests
{
    private static (Api.Data.AppDbContext db, EnrolmentsController controller) Setup(int userId, string role)
    {
        var db = TestHelpers.CreateDb();
        db.Users.AddRange(
            new User { UserId = 1, FullName = "Org", Email = "o@t.com", Role = "Organiser" },
            new User { UserId = 2, FullName = "Par", Email = "p@t.com", Role = "Participant" });
        db.Events.Add(new Event { EventId = 1, Name = "10K", Location = "CT", EventType = "Run", OrganiserId = 1 });
        db.Events.Add(new Event { EventId = 2, Name = "5K", Location = "CT", EventType = "Run", OrganiserId = 1 });
        db.Categories.Add(new Category { CategoryId = 1, Name = "Open", EventId = 1 });
        db.Categories.Add(new Category { CategoryId = 2, Name = "Open 5K", EventId = 2 });
        db.SaveChanges();

        var controller = new EnrolmentsController(db)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = TestHelpers.CreateHttpContext(userId, role)
            }
        };
        return (db, controller);
    }

    [Fact]
    public void Enrol_Valid_Succeeds()
    {
        var (db, c) = Setup(2, "Participant");
        var result = c.Enrol(new CreateEnrolmentRequest(1, 1));
        Assert.IsType<OkObjectResult>(result);
        Assert.Single(db.Enrolments);
    }

    [Fact]
    public void Enrol_Twice_ReturnsBadRequest()
    {
        var (_, c) = Setup(2, "Participant");
        c.Enrol(new CreateEnrolmentRequest(1, 1));
        var result = c.Enrol(new CreateEnrolmentRequest(1, 1));
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public void Enrol_CategoryFromOtherEvent_ReturnsBadRequest()
    {
        var (_, c) = Setup(2, "Participant");
        var result = c.Enrol(new CreateEnrolmentRequest(1, 2));
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public void GetEventEnrolments_NotOwner_Returns403()
    {
        var (db, _) = Setup(2, "Participant");
        db.Users.Add(new User { UserId = 3, FullName = "Other", Email = "x@t.com", Role = "Organiser" });
        db.SaveChanges();
        var other = new EnrolmentsController(db)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = TestHelpers.CreateHttpContext(3, "Organiser")
            }
        };

        var result = other.GetEventEnrolments(1);

        var obj = Assert.IsType<ObjectResult>(result);
        Assert.Equal(403, obj.StatusCode);
    }
}
