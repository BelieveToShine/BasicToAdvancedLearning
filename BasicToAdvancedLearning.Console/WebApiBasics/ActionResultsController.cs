using Microsoft.AspNetCore.Mvc;

namespace BasicToAdvancedLearning.WebApiBasics;

// Method 4's controller: an action decides the HTTP status by what it returns.
// The accounts live in a static list so the data survives from one request to the next (a new controller
// object is created for every request, so an ordinary field would be empty each time). Method 5 replaces
// this with a proper store object.
[ApiController]
[Route("api/accounts")]
public class ActionResultsController : ControllerBase
{
    private static readonly List<Account> Accounts = new();

    // Puts the two starting accounts back, so every run of the lesson begins the same.
    public static void ResetData()
    {
        Accounts.Clear();
        Accounts.Add(new Account { AccountId = 1, AccountNumber = "AC-1001", AccountType = "Savings", Balance = 500m });
        Accounts.Add(new Account { AccountId = 2, AccountNumber = "AC-1002", AccountType = "Current", Balance = 1200m });
    }

    // Returning the object itself: the framework wraps it as 200 OK + JSON. Simple, but this action can only ever answer 200 (unless it throws).
    [HttpGet]
    public List<Account> GetAll()
    {
        return Accounts;
    }

    // ActionResult<Account> allows BOTH: return the object (becomes 200) or return a status result such as NotFound().
    [HttpGet("{id:int}")]
    public ActionResult<Account> GetById(int id)
    {
        Account? account = Accounts.FirstOrDefault(a => a.AccountId == id);
        if (account == null)
        {
            return NotFound();   // 404
        }
        return account;          // 200
    }

    // IActionResult: the return type that can say "some result"; helper methods such as NotFound() and BadRequest() build it.
    // CreatedAtAction builds 201 Created, a Location header that points at GetById, and the new object as the body.
    [HttpPost]
    public IActionResult Create([FromBody] NewAccountRequest request)
    {
        if (request.AccountType != "Savings" && request.AccountType != "Current")
        {
            return BadRequest("AccountType must be Savings or Current");   // 400 with our own message
        }

        var account = new Account
        {
            AccountId = Accounts.Count + 1,   // demo only: after a delete this can repeat an id; a real store/database generates unique ids
            AccountNumber = request.AccountNumber,
            AccountType = request.AccountType,
            Balance = 0m,
        };
        Accounts.Add(account);
        return CreatedAtAction(nameof(GetById), new { id = account.AccountId }, account);
    }

    // NoContent() = 204: "it worked, and there is nothing to send back".
    [HttpDelete("{id:int}")]
    public IActionResult Delete(int id)
    {
        Account? account = Accounts.FirstOrDefault(a => a.AccountId == id);
        if (account == null)
        {
            return NotFound();
        }
        Accounts.Remove(account);
        return NoContent();
    }
}
