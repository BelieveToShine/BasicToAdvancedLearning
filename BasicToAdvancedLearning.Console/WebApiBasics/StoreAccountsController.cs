using Microsoft.AspNetCore.Mvc;

namespace BasicToAdvancedLearning.WebApiBasics;

// Method 5's controller. It does NOT write `new InMemoryAccountStore()`. It declares in its constructor what
// it needs (an IAccountStore) and the framework's DI container hands one over. That keeps the controller
// independent of WHICH store it gets, and lets whoever registers the service decide how long a store lives.
[ApiController]
[Route("api/accounts")]
public class StoreAccountsController : ControllerBase
{
    private readonly IAccountStore _store;

    // The container calls this constructor for EVERY request (it builds a fresh controller each time) and
    // passes in the store it has registered for IAccountStore.
    public StoreAccountsController(IAccountStore store)
    {
        _store = store;
        Console.WriteLine("    [server] a new StoreAccountsController was created");
    }

    [HttpGet]
    public IReadOnlyList<Account> GetAll() => _store.GetAll();

    [HttpPost]
    public IActionResult Create([FromBody] NewAccountRequest request)
    {
        Account account = _store.Add(request.AccountNumber, request.AccountType);
        return Created($"/api/accounts/{account.AccountId}", account);
    }
}
