using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace BasicToAdvancedLearning.WebApiCrud;

// CRUD over the Accounts table: Create = POST, Read = GET, Update = PUT, Delete = DELETE.
// [ApiController] turns on API behaviours: automatic 400 for invalid input, [FromBody] guessed for
// complex parameters, and ProblemDetails bodies for error status codes.
// The actions are synchronous to keep this lesson simple; real APIs usually use async, taught later.
[ApiController]
[Route("api/accounts")]
public class AccountsController : ControllerBase
{
    // SQL Server error numbers this controller turns into "409 Conflict".
    private const int ForeignKeyViolation = 547;       // also used for CHECK constraint violations
    private const int UniqueIndexViolation = 2601;     // duplicate in a unique index (what EF creates)
    private const int UniqueConstraintViolation = 2627; // duplicate in a UNIQUE constraint

    private readonly BankDbContext _db;

    // The controller asks for the context in its constructor; DI creates it (one per request).
    public AccountsController(BankDbContext db)
    {
        _db = db;
    }

    // The entity -> DTO conversion, written as a query so EF only selects the columns the DTO needs.
    public static IQueryable<AccountDto> Project(IQueryable<Account> accounts)
    {
        return accounts.Select(a => new AccountDto
        {
            AccountId = a.AccountId,
            CustomerName = a.Customer!.FullName,
            AccountNumber = a.AccountNumber,
            AccountType = a.AccountType,
            Balance = a.Balance,
        });
    }

    // GET /api/accounts  ->  200 and a JSON array (an empty array when there are no accounts).
    [HttpGet]
    public List<AccountDto> GetAll()
    {
        return Project(_db.Accounts.OrderBy(a => a.AccountId)).ToList();
    }

    // GET /api/accounts/{id}  ->  200 and one object, or 404 when there is no such account.
    [HttpGet("{id:int}")]
    public ActionResult<AccountDto> GetById(int id)
    {
        AccountDto? account = Project(_db.Accounts.Where(a => a.AccountId == id)).FirstOrDefault();
        if (account == null)
        {
            return NotFound();
        }

        return account;
    }

    // POST /api/accounts  ->  201 Created + a Location header, 400 for bad input, 409 for a duplicate.
    [HttpPost]
    public ActionResult<AccountDto> Create(CreateAccountRequest request)
    {
        // The attributes cannot know what is in the database, so the owner check is ours. Reporting it
        // through ModelState gives the same 400 shape as the automatic validation.
        if (!_db.Customers.Any(c => c.CustomerId == request.CustomerId))
        {
            ModelState.AddModelError(nameof(request.CustomerId), "No customer with that CustomerId.");
            return ValidationProblem(ModelState);
        }

        var account = new Account
        {
            CustomerId = request.CustomerId,
            AccountNumber = request.AccountNumber,
            AccountType = request.AccountType,
            Balance = request.Balance,
        };
        // The existence check above can go stale: a customer deleted right after it makes the foreign key refuse
        // the INSERT, and TrySave() turns that 547 into a 409.
        _db.Accounts.Add(account);

        if (!TrySave(out ActionResult? failure))
        {
            return failure!;
        }

        // After SaveChanges the database has generated AccountId. CreatedAtAction builds the Location
        // header (the address of the new resource, via GetById) and returns 201 with the new account.
        AccountDto created = Project(_db.Accounts.Where(a => a.AccountId == account.AccountId)).Single();
        return CreatedAtAction(nameof(GetById), new { id = account.AccountId }, created);
    }

    // PUT /api/accounts/{id}  ->  204 No Content, 404 when missing, 400 for bad input, 409 for a duplicate.
    // PUT means "replace the whole resource with what I sent". Like POST it can hit the duplicate AccountNumber 409.
    [HttpPut("{id:int}")]
    public IActionResult Replace(int id, ReplaceAccountRequest request)
    {
        Account? account = _db.Accounts.Find(id);   // Find loads the row and tracks it
        if (account == null)
        {
            return NotFound();
        }

        // Changing tracked properties is all EF needs; SaveChanges() turns the differences into an UPDATE.
        account.AccountNumber = request.AccountNumber;
        account.AccountType = request.AccountType;
        account.Balance = request.Balance;

        if (!TrySave(out ActionResult? failure))
        {
            return failure!;
        }

        return NoContent();
    }

    // DELETE /api/accounts/{id}  ->  204 No Content, 404 when missing, 409 when the account has history.
    [HttpDelete("{id:int}")]
    public IActionResult Delete(int id)
    {
        Account? account = _db.Accounts.Find(id);
        if (account == null)
        {
            return NotFound();
        }

        _db.Accounts.Remove(account);

        if (!TrySave(out ActionResult? failure))
        {
            return failure!;
        }

        return NoContent();
    }

    // Two requests changing the same row at once: the last write wins, and a row deleted in between makes
    // SaveChanges throw DbUpdateConcurrencyException, which this lesson does not handle (the client gets a 500).
    // Handling that (concurrency tokens) is taught later.
    // Saves, and turns the database's "no" into a 409 Conflict instead of a 500 crash. EF wraps the
    // SqlException inside a DbUpdateException, so the SQL Server error NUMBER is on the inner exception.
    private bool TrySave(out ActionResult? failure)
    {
        failure = null;
        try
        {
            _db.SaveChanges();
            return true;
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException sql && sql.Number == ForeignKeyViolation)
        {
            failure = Conflict(new ProblemDetails
            {
                Title = "Conflict",
                Status = StatusCodes.Status409Conflict,
                Detail = "Another row depends on this one (or this one points at a row that does not exist).",
            });
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException sql
            && (sql.Number == UniqueIndexViolation || sql.Number == UniqueConstraintViolation))
        {
            failure = Conflict(new ProblemDetails
            {
                Title = "Conflict",
                Status = StatusCodes.Status409Conflict,
                Detail = "That AccountNumber is already used by another account.",
            });
        }

        return false;
    }
}
