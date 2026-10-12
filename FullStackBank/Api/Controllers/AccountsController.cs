using FullStackBank.Api.Data;
using FullStackBank.Api.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FullStackBank.Api.Controllers;

// A controller is a class whose public methods ("actions") answer HTTP requests.
// [ApiController] switches on API conventions, the most important being: if the request body/query fails
// validation, the framework answers 400 with a ProblemDetails body itself and the action is not even called.
//
// async/await: real APIs use async so a thread is not blocked while waiting for the database and can serve
// other requests meanwhile. The C# lessons have not covered async/await yet; here read "await X" as
// "wait for X to finish, without blocking the thread".
[ApiController]
[Route("api/accounts")]
public class AccountsController : ControllerBase
{
    private readonly BankDbContext _db;

    public AccountsController(BankDbContext db)
    {
        // The framework creates one BankDbContext per request and passes it in (dependency injection).
        _db = db;
    }

    // GET /api/accounts            -> 200, all accounts
    // GET /api/accounts?type=Savings -> 200, only Savings (400 if type is not Savings/Current)
    [HttpGet]
    public async Task<ActionResult<List<AccountDto>>> GetAll([FromQuery] string? type)
    {
        IQueryable<Account> query = _db.Accounts;

        if (type is not null)
        {
            if (!Enum.TryParse(type, out AccountType parsed) || !Enum.IsDefined(parsed) || parsed.ToString() != type)
            {
                // Same error shape as the automatic validation: { "errors": { "type": [ ... ] } }.
                ModelState.AddModelError("type", "type must be 'Savings' or 'Current'.");
                return ValidationProblem(ModelState);
            }
            query = query.Where(a => a.AccountType == parsed);
        }

        // Select(...) fetches only the columns the DTO needs; no entity objects are created or tracked.
        // AsNoTracking is implied by projecting to a non-entity type.
        return await query
            .OrderBy(a => a.AccountId)
            .Select(ToDto)
            .ToListAsync();
    }

    // GET /api/accounts/{id} -> 200 or 404
    [HttpGet("{id:int}")]
    public async Task<ActionResult<AccountDto>> GetById(int id)
    {
        AccountDto? account = await _db.Accounts
            .Where(a => a.AccountId == id)
            .Select(ToDto)
            .FirstOrDefaultAsync();

        return account is null ? AccountNotFound(id) : account;
    }

    // POST /api/accounts -> 201 Created + Location header + the new account
    //   400 invalid body / unknown customer, 409 account number already taken
    [HttpPost]
    public async Task<ActionResult<AccountDto>> Create(CreateAccountRequest request)
    {
        // The customer must exist. It is the BODY that names a wrong customer (the URL is fine), so this is
        // a 400 validation error on "customerId", not a 404.
        if (!await _db.Customers.AnyAsync(c => c.CustomerId == request.CustomerId))
        {
            ModelState.AddModelError("customerId", $"Customer {request.CustomerId} does not exist.");
            return ValidationProblem(ModelState);
        }

        var account = new Account
        {
            CustomerId = request.CustomerId,
            AccountNumber = request.AccountNumber!,
            AccountType = Enum.Parse<AccountType>(request.AccountType!),   // safe: [AllowedValues] already checked it
            Balance = 0,
        };
        _db.Accounts.Add(account);

        // A duplicate account number is NOT pre-checked with a SELECT: two requests could both pass such a check.
        // The UNIQUE index is the real guard; if it fires, SaveChangesAsync throws and ApiExceptionHandler answers 409.
        await _db.SaveChangesAsync();

        AccountDto dto = (await _db.Accounts.Where(a => a.AccountId == account.AccountId).Select(ToDto).FirstAsync());

        // 201 + Location: "created, and you can fetch it at this URL".
        return CreatedAtAction(nameof(GetById), new { id = dto.AccountId }, dto);
    }

    // DELETE /api/accounts/{id} -> 204 No Content (body-less success)
    //   404 unknown account, 409 the account has transactions
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        Account? account = await _db.Accounts.FirstOrDefaultAsync(a => a.AccountId == id);
        if (account is null)
        {
            return AccountNotFound(id);
        }

        // A bank keeps its history: an account that has ever had a transaction cannot be deleted. This is a
        // CONFLICT with the current state of the data (409), not a bad request: the same request would have
        // succeeded had the account been empty. The foreign key (Restrict) would also stop it, but this check
        // lets us answer with a clear message instead of a generic database error.
        if (await _db.Transactions.AnyAsync(t => t.AccountId == id))
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Account has transactions",
                detail: $"Account {id} has transaction history and cannot be deleted.");
        }

        _db.Accounts.Remove(account);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // GET /api/accounts/{id}/transactions -> 200, newest first (404 if the account does not exist)
    [HttpGet("{id:int}/transactions")]
    public async Task<ActionResult<List<TransactionDto>>> GetTransactions(int id)
    {
        if (!await _db.Accounts.AnyAsync(a => a.AccountId == id))
        {
            return AccountNotFound(id);
        }

        // No paging yet: fine for a small learning bank, a real one would add ?page=&pageSize=.
        return await _db.Transactions
            .Where(t => t.AccountId == id)
            .OrderByDescending(t => t.TransactionId)
            .Select(t => new TransactionDto(t.TransactionId, t.Kind.ToString(), t.Amount, t.CreatedAt))
            .ToListAsync();
    }

    // GET /api/accounts/{id}/transactions/{transactionId} -> 200 or 404. Exists mainly so that the
    // POST below has a real URL to put in its Location header.
    [HttpGet("{id:int}/transactions/{transactionId:long}", Name = "GetTransaction")]
    public async Task<ActionResult<TransactionDto>> GetTransaction(int id, long transactionId)
    {
        TransactionDto? transaction = await _db.Transactions
            .Where(t => t.AccountId == id && t.TransactionId == transactionId)
            .Select(t => new TransactionDto(t.TransactionId, t.Kind.ToString(), t.Amount, t.CreatedAt))
            .FirstOrDefaultAsync();

        return transaction is null
            ? Problem(statusCode: StatusCodes.Status404NotFound, title: "Transaction not found", detail: $"Account {id} has no transaction {transactionId}.")
            : transaction;
    }

    // POST /api/accounts/{id}/transactions -> 201 Created + Location + TransactionDto
    //   400 invalid body (kind unknown, amount <= 0 or more than 2 decimals)
    //   404 unknown account
    //   422 insufficient funds
    //   409 two requests changed the account at the same moment
    [HttpPost("{id:int}/transactions")]
    public async Task<ActionResult<TransactionDto>> CreateTransaction(int id, CreateTransactionRequest request)
    {
        // Tracked entity (no AsNoTracking): we are going to change its Balance and want EF to notice.
        Account? account = await _db.Accounts.FirstOrDefaultAsync(a => a.AccountId == id);
        if (account is null)
        {
            return AccountNotFound(id);
        }

        var kind = Enum.Parse<TransactionKind>(request.Kind!);   // safe: [AllowedValues] already checked it

        if (kind == TransactionKind.Withdrawal && account.Balance < request.Amount)
        {
            // 422 Unprocessable Content vs 409 Conflict - why 422 here:
            //  * The request is well-formed and passes validation (so not 400), and the account exists (not 404).
            //  * What stops it is a BUSINESS RULE ("no overdraft") applied to this valid request.
            //  * 409 is used in this API for conflicts about the DATA's identity or concurrent change:
            //    duplicate account number, deleting an account that has history, a simultaneous update.
            // Both are defensible in the wild; the point is to choose a rule and apply it consistently.
            return Problem(
                statusCode: StatusCodes.Status422UnprocessableEntity,
                title: "Insufficient funds",
                detail: $"Account {id} has a balance of {account.Balance:0.00}; a withdrawal of {request.Amount:0.00} is not allowed.");
        }

        account.Balance += kind == TransactionKind.Deposit ? request.Amount : -request.Amount;
        var transaction = new BankTransaction
        {
            AccountId = id,
            Kind = kind,
            Amount = request.Amount,
            CreatedAt = DateTime.UtcNow,
        };
        _db.Transactions.Add(transaction);

        // ONE SaveChangesAsync = ONE database transaction: the balance UPDATE and the transaction INSERT either
        // both happen or neither does. (EF Core wraps all changes of a single SaveChanges call in a transaction.)
        // Without this, a crash between the two could leave money moved but no record of it.
        await _db.SaveChangesAsync();

        // Re-read the row we just wrote instead of echoing the request: the database stores DECIMAL(18,2), so this
        // answers amount 40.00 exactly like GET does (echoing the request would say 40 for a request that sent 40).
        TransactionDto result = await _db.Transactions
            .Where(t => t.TransactionId == transaction.TransactionId)
            .Select(t => new TransactionDto(t.TransactionId, t.Kind.ToString(), t.Amount, t.CreatedAt))
            .FirstAsync();
        return CreatedAtRoute("GetTransaction", new { id, transactionId = transaction.TransactionId }, result);
    }

    // An EXPRESSION (not a normal method) so EF can translate it to SQL and fetch only these columns.
    // A plain method inside Select would run in C# after loading whole entities.
    private static readonly System.Linq.Expressions.Expression<Func<Account, AccountDto>> ToDto = a =>
        new AccountDto(a.AccountId, a.Customer.FullName, a.AccountNumber, a.AccountType.ToString(), a.Balance);

    // Problem(...) builds a ProblemDetails (RFC 9457) body: { type, title, status, detail, traceId }.
    private ObjectResult AccountNotFound(int id) =>
        Problem(statusCode: StatusCodes.Status404NotFound, title: "Account not found", detail: $"There is no account with id {id}.");
}
