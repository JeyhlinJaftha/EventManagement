using EventManagement.Api.Auth;
using EventManagement.Api.Data;
using EventManagement.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace EventManagement.Api.Controllers;

[ApiController]
[Route("api/enrolments")]
public class EnrolmentsController : ControllerBase
{
    private readonly AppDbContext _context;

    public EnrolmentsController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>Enrols the logged-in Participant into an event.</summary>
    [HttpPost]
    [RequireRole("Participant")]
    public IActionResult Enrol(CreateEnrolmentRequest request)
    {
        int participantId = HttpContext.Session.GetInt32("UserId")!.Value;

        bool duplicate = _context.Enrolments.Any(e =>
            e.ParticipantId == participantId && e.EventId == request.EventId);

        if (duplicate)
        {
            return BadRequest("You are already enrolled in this event.");
        }

        bool validEvent = _context.Events.Any(e => e.EventId == request.EventId);
        bool validCategory = _context.Categories.Any(c =>
            c.CategoryId == request.CategoryId && c.EventId == request.EventId);

        if (!validEvent || !validCategory)
        {
            return BadRequest("The event or category is invalid.");
        }

        var enrolment = new Enrolment
        {
            ParticipantId = participantId,
            EventId = request.EventId,
            CategoryId = request.CategoryId,
            EnrolmentDate = DateTime.Now
        };

        _context.Enrolments.Add(enrolment);
        _context.SaveChanges();

        return Ok(new
        {
            enrolment.EnrolmentId,
            enrolment.ParticipantId,
            enrolment.EventId,
            enrolment.CategoryId,
            enrolment.EnrolmentDate
        });
    }

    /// <summary>Returns enrolments for an event. Only the owning Organiser may view them.</summary>
    [HttpGet("event/{eventId}")]
    [RequireRole("Organiser")]
    public IActionResult GetEventEnrolments(int eventId)
    {
        int organiserId = HttpContext.Session.GetInt32("UserId")!.Value;

        var eventItem = _context.Events.Find(eventId);
        if (eventItem == null) return NotFound("Event not found.");

        if (eventItem.OrganiserId != organiserId)
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                "You can only view enrolments for your own events.");
        }

        var enrolments = _context.Enrolments
            .Where(e => e.EventId == eventId)
            .Select(e => new
            {
                e.EnrolmentId,
                e.EnrolmentDate,
                Participant = new
                {
                    e.Participant!.UserId,
                    e.Participant.FullName,
                    e.Participant.Email,
                    e.Participant.PhoneNumber
                },
                Category = new { e.Category!.CategoryId, e.Category.Name }
            })
            .ToList();

        return Ok(enrolments);
    }
}

public record CreateEnrolmentRequest(int EventId, int CategoryId);
