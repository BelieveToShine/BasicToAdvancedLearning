using Microsoft.AspNetCore.Mvc;

namespace BasicToAdvancedLearning.WebApiBasics;

// Method 1's controller: the smallest useful one.
//  [ApiController]        - switches on API behaviour (automatic 400s, body binding guesses, ...).
//  [Route("api/[controller]")] - "[controller]" is replaced by the class name without "Controller",
//                           so this controller answers under /api/Accounts (matching ignores upper/lower case).
//  ControllerBase         - the base class for API controllers: NotFound(), BadRequest(), ... come from here.
[ApiController]
[Route("api/[controller]")]
public class AccountsController : ControllerBase
{
    // [HttpGet] = "run me for GET requests to this controller's route". The method name is free to choose.
    // Real APIs usually make actions async; that comes later in the course.
    [HttpGet]
    public string[] GetAccountNumbers()
    {
        return new[] { "AC-1001", "AC-1002" };
    }
}
