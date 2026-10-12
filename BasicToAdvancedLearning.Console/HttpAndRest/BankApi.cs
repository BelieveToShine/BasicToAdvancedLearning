using BasicToAdvancedLearning.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace BasicToAdvancedLearning.HttpAndRest;

// The lesson's tiny web server: a "minimal API" (each line of Map... connects one HTTP method + URL
// pattern to one small function). Accounts live in a list in memory, so every Start() begins with the
// same three accounts. A real API is a separate program that keeps running; the lesson hosts this one
// inside the console program only so the lesson can finish.
internal static class BankApi
{
    public const string AccountsPath = "/api/accounts";
    private const string CrashPath = "/api/crash";
    private const string OldAccountsPath = "/api/old-accounts";
    private const string InvalidInputMessage = "Owner is required and Balance cannot be negative.";

    // The same check for POST and PUT.
    private static bool IsValid(AccountInput input) => !string.IsNullOrWhiteSpace(input.Owner) && input.Balance >= 0;

    // Builds the server, adds the routes, starts it and returns it (with an HttpClient pointed at it).
    public static RunningServer Start()
    {
        var accounts = new List<Account>
        {
            new Account(1, "Asha Rao", 500.00m),
            new Account(2, "Ben Carter", 1200.50m),
            new Account(3, "Chen Li", 75.25m),
        };
        int nextId = accounts.Count + 1;
        // The list and nextId are not thread-safe; that is fine here because the lesson sends one request at a time.

        WebApplication app = WebLab.CreateBuilder().Build();

        // GET /api/accounts            -> all accounts, or only those whose owner contains the text in
        // ?owner=Asha (the match is case-sensitive)
        app.MapGet(AccountsPath, (string? owner) =>
            Results.Ok(accounts.Where(a => owner == null || a.Owner.Contains(owner)).ToList()));

        // GET /api/accounts/2          -> one account, or 404 when there is no such id
        // {id:int} means "this part of the URL must be a whole number".
        app.MapGet(AccountsPath + "/{id:int}", (int id) =>
        {
            Account? found = accounts.FirstOrDefault(a => a.Id == id);
            return found == null ? Results.NotFound() : Results.Ok(found);
        });

        // POST /api/accounts           -> create; 201 + Location header, or 400 when the input is bad
        app.MapPost(AccountsPath, (AccountInput input) =>
        {
            if (!IsValid(input))
            {
                return Results.BadRequest(InvalidInputMessage);
            }
            var created = new Account(nextId++, input.Owner!, input.Balance);
            accounts.Add(created);
            return Results.Created($"{AccountsPath}/{created.Id}", created);
        });

        // PUT /api/accounts/2          -> replace the whole account; 404 when there is no such id
        app.MapPut(AccountsPath + "/{id:int}", (int id, AccountInput input) =>
        {
            int index = accounts.FindIndex(a => a.Id == id);
            if (index < 0)
            {
                return Results.NotFound();
            }
            if (!IsValid(input))
            {
                return Results.BadRequest(InvalidInputMessage);
            }
            accounts[index] = new Account(id, input.Owner!, input.Balance);
            return Results.Ok(accounts[index]);
        });

        // DELETE /api/accounts/2       -> remove; 204 (success, nothing to send back), or 404 if already gone
        app.MapDelete(AccountsPath + "/{id:int}", (int id) =>
            accounts.RemoveAll(a => a.Id == id) == 0 ? Results.NotFound() : Results.NoContent());

        // A moved address (301) and a deliberately broken one (500), so the lesson can trigger real ones.
        app.MapGet(OldAccountsPath, () => Results.Redirect(AccountsPath, permanent: true));
        app.MapGet(CrashPath, (Func<IResult>)(() => throw new InvalidOperationException("The server code failed.")));

        return WebLab.Start(app);
    }
}
