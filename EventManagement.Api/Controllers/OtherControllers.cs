using EventManagement.Api.Auth;
using EventManagement.Api.Data;
using EventManagement.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace EventManagement.Api.Controllers;

// =====================================
// PROFILE CONTROLLER
// =====================================

[ApiController]
[Route("api/profile")]
public class ProfileController : ControllerBase
{
    private readonly AppDbContext _context;

    public ProfileController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>Returns the logged-in user's profile.</summary>
    [HttpGet]
    [RequireLogin]
    public IActionResult GetProfile()
    {
        int? userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null) return Unauthorized();

        var user = _context.Users.Find(userId.Value);
        if (user == null) return NotFound();

        return Ok(new { user.UserId, user.FullName, user.Email, user.Role, user.PhoneNumber });
    }

    /// <summary>Updates the logged-in user's own profile.</summary>
    [HttpPut]
    [RequireLogin]
    public IActionResult UpdateProfile(UpdateProfileRequest request)
    {
        int? userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null) return Unauthorized();

        var user = _context.Users.Find(userId.Value);
        if (user == null) return NotFound();

        if (_context.Users.Any(u => u.Email == request.Email && u.UserId != userId.Value))
        {
            return BadRequest("Email already registered.");
        }

        user.FullName = request.FullName;
        user.Email = request.Email;
        user.PhoneNumber = request.PhoneNumber;

        _context.SaveChanges();

        return Ok(new { user.UserId, user.FullName, user.Email, user.Role, user.PhoneNumber });
    }
}

// =====================================
// CATEGORIES CONTROLLER
// =====================================

[ApiController]
[Route("api/events/{eventId}/categories")]
public class CategoriesController : ControllerBase
{
    private readonly AppDbContext _context;

    public CategoriesController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>Returns all categories belonging to an event.</summary>
    [HttpGet]
    [RequireLogin]
    public IActionResult GetCategories(int eventId)
    {
        if (!_context.Events.Any(e => e.EventId == eventId))
        {
            return NotFound("Event not found.");
        }

        var categories = _context.Categories
            .Where(c => c.EventId == eventId)
            .Select(c => new { c.CategoryId, c.Name, c.EventId })
            .ToList();

        return Ok(categories);
    }

    /// <summary>Creates a category for an event. Organiser role required.</summary>
    [HttpPost]
    [RequireRole("Organiser")]
    public IActionResult CreateCategory(int eventId, CreateCategoryRequest request)
    {
        int organiserId = HttpContext.Session.GetInt32("UserId")!.Value;

        var eventItem = _context.Events.Find(eventId);
        if (eventItem == null) return NotFound("Event not found.");

        if (eventItem.OrganiserId != organiserId)
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                "You can only create categories for your own events.");
        }

        var category = new Category { Name = request.Name, EventId = eventId };

        _context.Categories.Add(category);
        _context.SaveChanges();

        return Ok(new { category.CategoryId, category.Name, category.EventId });
    }
}

// =====================================
// RESULTS CONTROLLER
// =====================================

[ApiController]
[Route("api/results")]
public class ResultsController : ControllerBase
{
    private readonly AppDbContext _context;

    public ResultsController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>Captures a result for an enrolment. Organiser role required.</summary>
    [HttpPost]
    [RequireRole("Organiser")]
    public IActionResult CaptureResult(CreateResultRequest request)
    {
        int organiserId = HttpContext.Session.GetInt32("UserId")!.Value;

        var enrolment = _context.Enrolments
            .Where(e => e.EnrolmentId == request.EnrolmentId)
            .Select(e => new { e.EnrolmentId, OrganiserId = e.Event!.OrganiserId })
            .FirstOrDefault();

        if (enrolment == null) return BadRequest("Enrolment does not exist.");

        if (enrolment.OrganiserId != organiserId)
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                "You can only capture results for your own events.");
        }

        if (_context.Results.Any(r => r.EnrolmentId == request.EnrolmentId))
        {
            return BadRequest("A result already exists for this enrolment.");
        }

        var result = new Result
        {
            EnrolmentId = request.EnrolmentId,
            FinishTime = request.FinishTime,
            Position = request.Position
        };

        _context.Results.Add(result);
        _context.SaveChanges();

        return Ok(new { result.ResultId, result.EnrolmentId, result.FinishTime, result.Position });
    }

    /// <summary>Returns results belonging to the logged-in Participant.</summary>
    [HttpGet("mine")]
    [RequireRole("Participant")]
    public IActionResult GetMyResults()
    {
        int participantId = HttpContext.Session.GetInt32("UserId")!.Value;

        var results = _context.Results
            .Where(r => r.Enrolment != null && r.Enrolment.ParticipantId == participantId)
            .Select(r => new
            {
                r.ResultId,
                r.FinishTime,
                r.Position,
                EventName = r.Enrolment!.Event!.Name,
                EventDate = r.Enrolment.Event.EventDate
            })
            .ToList();

        return Ok(results);
    }
}

// =====================================
// REQUEST CLASSES
// =====================================

public record CreateCategoryRequest(string Name);

public record CreateResultRequest(int EnrolmentId, TimeSpan FinishTime, int Position);

public record UpdateProfileRequest(string FullName, string Email, string? PhoneNumber);
