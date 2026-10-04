using EventManagement.Api.Data;
using EventManagement.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace EventManagement.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;

    public AuthController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>Registers a new Organiser or Participant.</summary>
    [HttpPost("register")]
    public IActionResult Register(RegisterRequest request)
    {
        if (request.Role is not ("Organiser" or "Participant"))
        {
            return BadRequest("Role must be Organiser or Participant.");
        }

        if (_context.Users.Any(u => u.Email == request.Email))
        {
            return BadRequest("Email already registered.");
        }

        var user = new User
        {
            FullName = request.FullName,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = request.Role,
            PhoneNumber = request.PhoneNumber
        };

        _context.Users.Add(user);
        _context.SaveChanges();

        return Ok("Registration successful.");
    }

    /// <summary>Logs the user in and starts a session.</summary>
    [HttpPost("login")]
    public IActionResult Login(LoginRequest request)
    {
        var user = _context.Users.SingleOrDefault(u => u.Email == request.Email);

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return Unauthorized("Invalid email or password.");
        }

        HttpContext.Session.SetInt32("UserId", user.UserId);
        HttpContext.Session.SetString("Role", user.Role);

        return Ok(new { user.UserId, user.FullName, user.Role });
    }

    /// <summary>Logs the user out and clears the session.</summary>
    [HttpPost("logout")]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return Ok("Logged out.");
    }
}

public record RegisterRequest(
    string FullName,
    string Email,
    string Password,
    string Role,
    string? PhoneNumber);

public record LoginRequest(string Email, string Password);
