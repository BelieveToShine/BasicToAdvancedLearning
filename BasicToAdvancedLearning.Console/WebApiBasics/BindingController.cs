using Microsoft.AspNetCore.Mvc;

namespace BasicToAdvancedLearning.WebApiBasics;

// Method 3's controller: where does each action parameter's value come from?
[ApiController]
[Route("api/accounts")]
public class BindingController : ControllerBase
{
    // `id` comes from the ROUTE ({id:int}); `last` from the QUERY STRING (?last=3). [FromQuery] says so
    // explicitly. The default value 5 is used when the client leaves ?last out.
    [HttpGet("{id:int}/statement")]
    public string Statement(int id, [FromQuery] int last = 5)
    {
        return $"statement for account {id}: last {last} transactions";
    }

    // `id` from the route, `request` from the JSON BODY ([FromBody]). ASP.NET Core reads the body, turns the
    // JSON into a DepositRequest object, and checks its [Range] rule before this method runs.
    [HttpPost("{id:int}/deposits")]
    public string Deposit([FromRoute] int id, [FromBody] DepositRequest request)
    {
        return $"deposit {request.Amount} into account {id}";
    }
}

// The SAME two actions again, but WITHOUT [ApiController] - to show what that attribute adds.
// Nothing here rejects bad input and the JSON body is not picked up for a class-type parameter.
[Route("api/plain")]
public class PlainBindingController : ControllerBase
{
    [HttpGet("{id:int}/statement")]
    public string Statement(int id, int last = 5)
    {
        return $"statement for account {id}: last {last} transactions";
    }

    [HttpPost("{id:int}/deposits")]
    public string Deposit(int id, DepositRequest request)
    {
        return $"deposit {request.Amount} into account {id}";
    }
}
