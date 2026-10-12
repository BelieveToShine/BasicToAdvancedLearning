using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BasicToAdvancedLearning.JwtAuth;

// What a client sends to /login. (public because MVC controllers and their parameter types must be public)
public sealed record LoginRequest(string UserName, string Password);

// The few endpoints of this lesson's pretend bank API. Real APIs usually use async actions (taught later);
// these are synchronous to keep the lesson simple.
[ApiController]
public sealed class AuthDemoController : ControllerBase
{
    // Anyone may call this: it is how you GET a token. [AllowAnonymous] says "skip the login check here".
    [AllowAnonymous]
    [HttpPost("/login")]
    public IActionResult Login([FromBody] LoginRequest request, [FromServices] TokenIssuer issuer, [FromServices] DemoUsers users)
    {
        DemoUser? user = users.CheckLogin(request.UserName, request.Password);
        if (user == null)
        {
            return Unauthorized();   // same answer for "no such user" and "wrong password"
        }

        DateTime now = DateTime.UtcNow;
        string token = issuer.Issue(user, now, now.AddMinutes(TokenIssuer.LoginLifetimeMinutes));
        return Ok(new { token });
    }

    // Explicitly public, so the lesson can contrast it with the protected endpoints below.
    [AllowAnonymous]
    [HttpGet("/api/status")]
    public IActionResult Status() => Ok("The bank is open.");

    // [Authorize] alone = "any logged-in caller" (any valid token), whatever the role.
    [Authorize]
    [HttpGet("/api/accounts/me")]
    public IActionResult Me()
    {
        // User is filled in by the authentication step from the token's claims.
        return Ok($"Hello {User.Identity?.Name}, your role is {User.FindFirst("role")?.Value}.");
    }

    // [Authorize(Roles = ...)] = "logged in AND has this role". A valid token with another role is refused.
    [Authorize(Roles = DemoUsers.ManagerRole)]
    [HttpGet("/api/reports/daily")]
    public IActionResult DailyReport() => Ok("Daily report: 3 accounts opened.");
}
