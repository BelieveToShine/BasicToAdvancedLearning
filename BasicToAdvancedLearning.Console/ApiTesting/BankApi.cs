using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;
using BasicToAdvancedLearning.Interfaces;

namespace BasicToAdvancedLearning.ApiTesting;

// The small "bank" Web API that Topic 19 documents and tests. It keeps its accounts in a list in memory
// (no database), so every lesson method that starts a fresh server begins with the same two accounts.
// This file is helper code: the lesson itself is in ApiTestingDemo.cs.

// One bank account as the API sends it back. A plain class so the JSON shape is easy to read.
public class AccountDto
{
    public int Id { get; set; }
    public string Owner { get; set; } = "";
    public string AccountType { get; set; } = "";
    public decimal Balance { get; set; }
}

// The body a client sends to open an account. The attributes are rules the API checks for us, and
// they also show up in the OpenAPI document (Swagger UI reads them from there).
public class CreateAccountRequest
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Owner { get; set; } = "";

    [Required]
    [RegularExpression("^(Savings|Current)$")]
    public string AccountType { get; set; } = "";
}

// The body a client sends to deposit money.
public class DepositRequest
{
    [Range(0.01, 1000000)]
    public decimal Amount { get; set; }
}

// The in-memory "database": a list guarded by a lock. One instance per server (registered as a singleton).
public class AccountStore
{
    private readonly object _gate = new();
    private readonly List<AccountDto> _accounts = new()
    {
        new AccountDto { Id = 1, Owner = "Asha Rao", AccountType = "Savings", Balance = 500m },
        new AccountDto { Id = 2, Owner = "Ben Carter", AccountType = "Current", Balance = 120m },
    };

    public List<AccountDto> All() { lock (_gate) { return _accounts.Select(Copy).ToList(); } }

    public AccountDto? Find(int id) { lock (_gate) { AccountDto? a = _accounts.FirstOrDefault(x => x.Id == id); return a == null ? null : Copy(a); } }

    public AccountDto Add(string owner, string accountType)
    {
        lock (_gate)
        {
            var created = new AccountDto { Id = _accounts.Max(x => x.Id) + 1, Owner = owner, AccountType = accountType, Balance = 0m };
            _accounts.Add(created);
            return Copy(created);
        }
    }

    public AccountDto? Deposit(int id, decimal amount)
    {
        lock (_gate)
        {
            AccountDto? a = _accounts.FirstOrDefault(x => x.Id == id);
            if (a == null) return null;
            a.Balance += amount;
            return Copy(a);
        }
    }

    public bool Remove(int id) { lock (_gate) { return _accounts.RemoveAll(x => x.Id == id) > 0; } }

    private static AccountDto Copy(AccountDto a) => new() { Id = a.Id, Owner = a.Owner, AccountType = a.AccountType, Balance = a.Balance };
}

// The controller. Actions are synchronous to keep the lesson simple; real APIs usually use async,
// which is taught later. [ProducesResponseType] tells the documentation which status codes an action can
// return; it does not change what the action does.
[ApiController]
[Route("api/accounts")]
public class AccountsController : ControllerBase
{
    private readonly AccountStore _store;
    public AccountsController(AccountStore store) => _store = store;

    [HttpGet]
    [EndpointSummary("Lists every account")]
    [ProducesResponseType(typeof(List<AccountDto>), StatusCodes.Status200OK)]
    public IActionResult ListAccounts() => Ok(_store.All());

    [HttpGet("{id:int}")]
    [EndpointSummary("Gets one account by its id")]
    [EndpointDescription("Returns 404 when no account has that id.")]
    [ProducesResponseType(typeof(AccountDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetAccount(int id)
    {
        AccountDto? account = _store.Find(id);
        return account == null ? NotFound() : Ok(account);
    }

    [HttpPost]
    [EndpointSummary("Opens a new account with a zero balance")]
    [ProducesResponseType(typeof(AccountDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public IActionResult CreateAccount(CreateAccountRequest request)
    {
        AccountDto created = _store.Add(request.Owner, request.AccountType);
        return CreatedAtAction(nameof(GetAccount), new { id = created.Id }, created);
    }

    [HttpPost("{id:int}/deposit")]
    [EndpointSummary("Adds money to an account")]
    [ProducesResponseType(typeof(AccountDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult Deposit(int id, DepositRequest request)
    {
        AccountDto? updated = _store.Deposit(id, request.Amount);
        return updated == null ? NotFound() : Ok(updated);
    }

    // Deliberately NOT documented: no [ProducesResponseType] here, to show what the OpenAPI document
    // says when the author does not describe the responses (see ExplainDocumentingEndpoints).
    [HttpDelete("{id:int}")]
    public IActionResult DeleteAccount(int id) => _store.Remove(id) ? NoContent() : NotFound();
}

// Builds and starts the bank API on a free local port (shared WebLab helper). Because of the free port the
// lesson never prints the address, only paths such as GET /api/accounts/1.
internal static class BankApi
{
    public static RunningServer Start()
    {
        WebApplicationBuilder builder = WebLab.CreateBuilder();
        builder.Services.AddSingleton<AccountStore>();
        builder.Services.AddControllersOnly(typeof(AccountsController));

        // The two Swagger services: the first lets Swashbuckle see the controller actions, the second
        // builds the OpenAPI document from them.
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
            options.SwaggerDoc("v1", new OpenApiInfo { Title = "Bank API", Version = "v1" }));

        WebApplication app = builder.Build();
        app.UseSwagger();     // serves the JSON document at /swagger/v1/swagger.json
        app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", "Bank API v1"));   // the web page at /swagger
        app.MapControllers();
        return WebLab.Start(app);
    }
}
