using EventManagement.Api.Auth;
using EventManagement.Api.Data;
using EventManagement.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace EventManagement.Api.Controllers;

[ApiController]
[Route("api/events")]
public class EventsController : ControllerBase
{
    private readonly AppDbContext _context;

    public EventsController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>Returns all events.</summary>
    [HttpGet]
    [RequireLogin]
    public IActionResult GetEvents()
    {
        return Ok(_context.Events.ToList());
    }

    /// <summary>Returns one event.</summary>
    [HttpGet("{id}")]
    [RequireLogin]
    public IActionResult GetEvent(int id)
    {
        var item = _context.Events.Find(id);
        if (item == null) return NotFound();
        return Ok(item);
    }

    /// <summary>Creates a new event. Organiser role required.</summary>
    [HttpPost]
    [RequireRole("Organiser")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult CreateEvent(EventRequest request)
    {
        int organiserId = HttpContext.Session.GetInt32("UserId")!.Value;

        var item = new Event
        {
            Name = request.Name,
            Description = request.Description,
            EventDate = request.EventDate,
            Location = request.Location,
            Distance = request.Distance,
            EventType = request.EventType,
            OrganiserId = organiserId
        };

        _context.Events.Add(item);
        _context.SaveChanges();

        return CreatedAtAction(nameof(GetEvent), new { id = item.EventId }, item);
    }

    /// <summary>Updates an event. Only the owning Organiser may update it.</summary>
    [HttpPut("{id}")]
    [RequireRole("Organiser")]
    public IActionResult UpdateEvent(int id, EventRequest request)
    {
        int organiserId = HttpContext.Session.GetInt32("UserId")!.Value;

        var item = _context.Events.Find(id);
        if (item == null) return NotFound();

        if (item.OrganiserId != organiserId)
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                "You can only update your own events.");
        }

        item.Name = request.Name;
        item.Description = request.Description;
        item.EventDate = request.EventDate;
        item.Location = request.Location;
        item.Distance = request.Distance;
        item.EventType = request.EventType;

        _context.SaveChanges();
        return Ok(item);
    }

    /// <summary>Deletes an event. Only the owning Organiser may delete it.</summary>
    [HttpDelete("{id}")]
    [RequireRole("Organiser")]
    public IActionResult DeleteEvent(int id)
    {
        int organiserId = HttpContext.Session.GetInt32("UserId")!.Value;

        var item = _context.Events.Find(id);
        if (item == null) return NotFound();

        if (item.OrganiserId != organiserId)
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                "You can only delete your own events.");
        }

        _context.Events.Remove(item);
        _context.SaveChanges();
        return NoContent();
    }
}

public class EventRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime EventDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public decimal Distance { get; set; }
    public string EventType { get; set; } = string.Empty;
}
