using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace FullStackBank.Api.Infrastructure;

// The ONE place where "something threw an exception" becomes an HTTP answer. Registered with
// AddExceptionHandler + UseExceptionHandler in Program.cs, so it runs in every environment (including
// Production), not only in Development.
//
// Rule: a client must never see a stack trace, SQL text, table names or the server's file paths. Those help an
// attacker and mean nothing to a user. The full exception is written to the server log instead; the client gets
// a short, generic ProblemDetails plus a traceId it can quote to whoever reads the log.
public class ApiExceptionHandler : IExceptionHandler
{
    // SQL Server error NUMBERS (stable across versions, unlike the message text).
    private const int ForeignKeyOrCheckViolation = 547;     // FK / CHECK constraint conflict
    private const int UniqueIndexViolation = 2601;          // duplicate key in a unique INDEX
    private const int UniqueConstraintViolation = 2627;     // duplicate key in a unique CONSTRAINT

    private readonly IProblemDetailsService _problemDetails;
    private readonly ILogger<ApiExceptionHandler> _logger;

    public ApiExceptionHandler(IProblemDetailsService problemDetails, ILogger<ApiExceptionHandler> logger)
    {
        _problemDetails = problemDetails;
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        ProblemDetails problem = Map(exception);

        if (problem.Status == StatusCodes.Status500InternalServerError)
        {
            // Unexpected: log everything (with the stack trace) for the developers.
            _logger.LogError(exception, "Unhandled exception for {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
        }
        else
        {
            // Expected, a client-side conflict: a warning without the stack trace is enough.
            _logger.LogWarning("Database conflict for {Method} {Path}: {Title}", httpContext.Request.Method, httpContext.Request.Path, problem.Title);
        }

        httpContext.Response.StatusCode = problem.Status!.Value;
        return await _problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception,
        });
    }

    private static ProblemDetails Map(Exception exception)
    {
        // Two requests changed the same account at the same moment (see Account.RowVersion). Not the client's
        // fault and nothing was saved; retrying with fresh data normally succeeds.
        // (Checked first: DbUpdateConcurrencyException is a kind of DbUpdateException.)
        if (exception is DbUpdateConcurrencyException)
        {
            return Conflict("The record was changed by someone else",
                "Another request changed this account at the same time and nothing was saved. Read the account again and retry.");
        }

        // EF wraps the SqlException inside a DbUpdateException; its Number says which rule the database enforced.
        if (exception is DbUpdateException { InnerException: SqlException sql })
        {
            switch (sql.Number)
            {
                case UniqueIndexViolation:
                case UniqueConstraintViolation:
                    return Conflict("Duplicate value", "A record with the same unique value (for example the account number) already exists.");
                // 547 covers FOREIGN KEY and CHECK violations. The CHECK constraints (balance >= 0, amount > 0, ...) are a
                // last-line safety net: the app validates first, so in practice only the foreign-key case can be reached
                // (for example a customer or account removed by someone else between our check and our insert).
                case ForeignKeyOrCheckViolation:
                    return Conflict("The change conflicts with related data", "The database refused the change because it breaks a relationship or a rule between records.");
            }
        }

        // Anything else is a bug or an outage (database down, ...). Generic on purpose: no exception message, no type name.
        return new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "An unexpected error occurred",
            Detail = "Something went wrong on the server. Please try again later.",
        };
    }

    private static ProblemDetails Conflict(string title, string detail) => new()
    {
        Status = StatusCodes.Status409Conflict,
        Title = title,
        Detail = detail,
    };
}
