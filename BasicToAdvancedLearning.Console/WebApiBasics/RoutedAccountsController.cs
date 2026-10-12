using Microsoft.AspNetCore.Mvc;

namespace BasicToAdvancedLearning.WebApiBasics;

// Method 2's controller: several actions under one prefix, each with its own route template.
// The prefix is written out ("api/accounts") instead of using [controller], because this class is
// called RoutedAccountsController and we want the same public URL as the other lesson controllers.
[ApiController]
[Route("api/accounts")]
public class RoutedAccountsController : ControllerBase
{
    // Template "{id:int:min(1)}": the segment is captured into the parameter `id`, and two constraints
    // must hold - it parses as an int, and it is at least 1. If a constraint fails this route does not match.
    [HttpGet("{id:int:min(1)}")]
    public string GetById(int id)
    {
        return $"account {id}";
    }

    // A route can mix fixed words ("by-number") and parameters. No constraint: any text matches.
    [HttpGet("by-number/{number}")]
    public string GetByNumber(string number)
    {
        return $"account number {number}";
    }
}

// Two actions with the SAME template: the router cannot choose, so a request to it fails (method 2 shows how).
[ApiController]
[Route("api/clash")]
public class ClashController : ControllerBase
{
    [HttpGet]
    public string First() => "first";

    [HttpGet]
    public string Second() => "second";
}
