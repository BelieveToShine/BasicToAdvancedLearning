using System.Net.Http.Json;
using System.Text.Json;
using BasicToAdvancedLearning.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BasicToAdvancedLearning.WebApiCrud;

// Topic 18: CRUD endpoints (Create, Read, Update, Delete) backed by EF Core + SQL Server. The running
// example is the bank: an AccountsController over the Customers / Accounts / Transactions tables.
// A real API is a separate program that keeps running and waits for requests. To keep this lesson a
// console program that finishes, the web server is hosted INSIDE this process (WebLab) and the lesson
// plays the client with HttpClient — what the server does is exactly what a deployed API does.
// The methods build on each other: the first starts the server and seeds the data, the others send
// requests to it. Dispose() at the end stops the server.
// WARNING: the first method calls Database.EnsureDeleted(), which drops the ENTIRE database named in the
// connection string it is given. Here that is this lesson's own scratch database; never point it at a
// database holding data you care about.
// See ArchitectureDiagrams/WebApiCrud for the visual walkthrough.
public class WebApiCrudDemo : IDisposable
{
    private const string AccountsPath = "/api/accounts";
    private const string SqlSeeAccounts = "SELECT AccountId, CustomerId, AccountNumber, AccountType, Balance FROM Accounts ORDER BY AccountId";

    private WebApplication? _app;
    private RunningServer? _server;

    // DI = dependency injection: the app keeps a list of "how to build X" recipes (services) and hands
    // finished objects to whoever asks. AddDbContext adds the recipe for our DbContext.
    public void ExplainDbContextInDI()
    {
        PrintHeading("1. DbContext in dependency injection — one context per request");

        WebApplicationBuilder builder = WebLab.CreateBuilder();

        // AddDbContext registers BankDbContext with the SCOPED lifetime by default: one instance per
        // scope, and the web server opens one scope for each incoming request.
        builder.Services.AddDbContext<BankDbContext>(options => options.UseSqlServer(SqlLab.ConnectionString));
        builder.Services.AddControllersOnly(typeof(AccountsController));

        _app = builder.Build();
        _app.MapControllers();

        // Prove the lifetime: ask for the context twice inside one scope, then once inside another.
        using (IServiceScope scopeA = _app.Services.CreateScope())
        {
            var first = scopeA.ServiceProvider.GetRequiredService<BankDbContext>();
            var second = scopeA.ServiceProvider.GetRequiredService<BankDbContext>();
            Console.WriteLine($"Same scope, asked twice  -> same object? {ReferenceEquals(first, second)}");

            using (IServiceScope scopeB = _app.Services.CreateScope())
            {
                var other = scopeB.ServiceProvider.GetRequiredService<BankDbContext>();
                Console.WriteLine($"Different scope          -> same object? {ReferenceEquals(first, other)}");
            }

            // This scope also sets up the data. WARNING: EnsureDeleted drops the ENTIRE database named in the
            // connection string it is given. Here that is this lesson's own scratch database (SqlLab makes one
            // per lesson); on a database holding data you care about it would delete everything. EnsureCreated
            // then builds the tables again from the model (BankDbContext.OnModelCreating), so every run starts
            // the same. Real apps use migrations to change a database; these calls are for throwaway databases.
            Console.WriteLine("WARNING: EnsureDeleted drops the ENTIRE database in the connection string (here: this lesson's own scratch database).");
            first.Database.EnsureDeleted();
            first.Database.EnsureCreated();
            SeedBank(first);
        }

        // Why one per request: a DbContext remembers every entity it loaded (its change tracker) and is
        // not safe to use from two threads at once. A fresh one per request means two requests never share
        // tracked entities or half-built changes in memory, and nothing leaks into the next request.
        Console.WriteLine("Database recreated and seeded. Accounts table now:");
        SqlLab.Show(SqlSeeAccounts);

        // Start() begins listening; from here on the controller can answer requests.
        _server = WebLab.Start(_app);
        Console.WriteLine("Server is listening.");
    }

    // GET is the "Read" in CRUD. The controller never returns the entity; it returns a DTO.
    public void ExplainReadEndpoints()
    {
        PrintHeading("2. Read endpoints — GET, 404, and DTOs instead of entities");

        // Why a DTO and not the entity: (1) the response contains exactly the fields we chose, so adding a
        // column to the table later does not silently leak into the API; (2) the entities point at each
        // other (Customer -> Accounts -> Customer), and when both sides are loaded the default JSON
        // serializer throws "possible object cycle" (the request ends as a 500);
        // (3) the request side needs its own shape anyway (see the next method).
        ShowGeneratedSql();

        // Each call below is a real HTTP request. Names are camelCase in the JSON because ASP.NET Core's
        // default JSON settings do that.
        ApiReply list = Call(HttpMethod.Get, AccountsPath);
        ApiReply one = Call(HttpMethod.Get, AccountsPath + "/1");
        ApiReply missing = Call(HttpMethod.Get, AccountsPath + "/99");
        Console.WriteLine($"Status numbers: list {list.StatusCode}, one {one.StatusCode}, missing {missing.StatusCode}.");

        // Separately: the 404 above carries a body too.
        Console.WriteLine("The 404 body is a ProblemDetails (the standard shape for HTTP errors) because of [ApiController].");
    }

    // POST is the "Create". Validation attributes on the request DTO reject bad input before the action runs.
    public void ExplainCreateEndpoint()
    {
        PrintHeading("3. Create endpoint — POST, validation, 201 + Location");

        // A valid request: the answer is 201 Created, the new resource in the body, and a Location header
        // holding the address of the new resource.
        var openAccount = new CreateAccountRequest { CustomerId = 3, AccountNumber = "ACC-1004", AccountType = "Current", Balance = 20m };
        ApiReply created = Call(HttpMethod.Post, AccountsPath, openAccount);

        // The Location header lets the client fetch what it just created.
        Console.WriteLine($"Following the Location header: GET {created.LocationPath}");
        Call(HttpMethod.Get, created.LocationPath!);

        // An invalid request: three rules broken (too short, not an allowed type, negative balance). The
        // action method never ran: [ApiController] answered 400 with every broken rule listed.
        var badRequest = new CreateAccountRequest { CustomerId = 1, AccountNumber = "A1", AccountType = "Gold", Balance = -5m };
        Call(HttpMethod.Post, AccountsPath, badRequest);

        // The attributes cannot see the database. Two checks that need it are written in the action:
        // the owner must exist (400), and the account number must be unused (409, from the unique index).
        var unknownOwner = new CreateAccountRequest { CustomerId = 99, AccountNumber = "ACC-2000", AccountType = "Savings", Balance = 0m };
        Call(HttpMethod.Post, AccountsPath, unknownOwner);
        var duplicateNumber = new CreateAccountRequest { CustomerId = 2, AccountNumber = "ACC-1001", AccountType = "Savings", Balance = 0m };
        Call(HttpMethod.Post, AccountsPath, duplicateNumber);

        Console.WriteLine("Accounts table afterwards (the bad requests added nothing):");
        SqlLab.Show(SqlSeeAccounts);
    }

    // PUT is the "Update" and DELETE the "Delete". Both can meet 404; DELETE can also meet a foreign key.
    public void ExplainUpdateAndDelete()
    {
        PrintHeading("4. Update and delete — PUT, DELETE, 204, 404 and 409");

        // PUT replaces the whole account with what the client sent. Success is 204 No Content: nothing
        // more to say, so no body.
        var replacement = new ReplaceAccountRequest { AccountNumber = "ACC-1002", AccountType = "Savings", Balance = 1500m };
        ApiReply replaced = Call(HttpMethod.Put, AccountsPath + "/2", replacement);
        Console.WriteLine($"The body of that 204 is {replaced.Body.Length} characters long.");
        Call(HttpMethod.Get, AccountsPath + "/2");
        Call(HttpMethod.Put, AccountsPath + "/99", replacement);

        // Account 3 has no transactions, so it can go. A second DELETE finds nothing: 404.
        Call(HttpMethod.Delete, AccountsPath + "/3");
        Call(HttpMethod.Delete, AccountsPath + "/3");

        // Account 1 has transactions pointing at it (a foreign key). First the raw database answer, then
        // what the API makes of it: 409 Conflict, and the row is still there.
        Console.WriteLine("The same delete sent straight to SQL Server:");
        ShowRawDeleteRefused();
        ApiReply blocked = Call(HttpMethod.Delete, AccountsPath + "/1");
        Console.WriteLine($"The API answered {blocked.StatusCode} instead of crashing with a 500.");

        Console.WriteLine("Accounts table afterwards:");
        SqlLab.Show(SqlSeeAccounts);
        Console.WriteLine("Transactions table afterwards (untouched):");
        SqlLab.Show("SELECT TransactionId, AccountId, Amount, Kind FROM Transactions ORDER BY TransactionId");
    }

    // Stops the in-process server (a real API would just keep running).
    public void Dispose()
    {
        _server?.Dispose();
    }

    // Prints the SQL that EF Core builds for the DTO query. The Select(...) builds the DTO INSIDE the query, so
    // SQL Server only sends the columns the DTO needs, and the owner's name arrives through a JOIN.
    // ToQueryString() only builds the text; nothing is sent to the database.
    private void ShowGeneratedSql()
    {
        using IServiceScope scope = _app!.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BankDbContext>();
        string sql = AccountsController.Project(db.Accounts).ToQueryString();
        Console.WriteLine("EF Core turns the Select(...) into this SQL:");
        Console.WriteLine(sql);
    }

    // Runs the DELETE with no API in between, to show the number SQL Server answers with. 547 is SQL Server's
    // error number for a violated FOREIGN KEY or CHECK constraint; the wording of the message can differ
    // between versions, so the lesson prints only the number.
    private static void ShowRawDeleteRefused()
    {
        try
        {
            SqlLab.Run("DELETE FROM Accounts WHERE AccountId = 1");
            Console.WriteLine("  (no error - the statement succeeded)");
        }
        catch (SqlException ex)
        {
            Console.WriteLine($"  SQL Server refused it: error number {ex.Number} (a foreign key points at this row)");
        }
    }

    // The bank's starting rows: three customers, one account each, and some history on accounts 1 and 2.
    private static void SeedBank(BankDbContext db)
    {
        db.Customers.AddRange(
            new Customer { FullName = "Asha Rao", Email = "asha@example.com" },
            new Customer { FullName = "Ben Carter", Email = "ben@example.com" },
            new Customer { FullName = "Chen Li", Email = "chen@example.com" });
        db.SaveChanges();   // customers get their ids (1, 2, 3) here

        db.Accounts.AddRange(
            new Account { CustomerId = 1, AccountNumber = "ACC-1001", AccountType = "Savings", Balance = 500m },
            new Account { CustomerId = 2, AccountNumber = "ACC-1002", AccountType = "Current", Balance = 1200.50m },
            new Account { CustomerId = 3, AccountNumber = "ACC-1003", AccountType = "Savings", Balance = 75m });
        db.SaveChanges();

        db.Transactions.AddRange(
            new BankTransaction { AccountId = 1, Amount = 500m, Kind = "Deposit" },
            new BankTransaction { AccountId = 2, Amount = 1200.50m, Kind = "Deposit" });
        db.SaveChanges();
    }

    // Sends ONE real HTTP request to the in-process server, prints "METHOD path -> status" and the body.
    private ApiReply Call(HttpMethod method, string path, object? body = null)
    {
        using var request = new HttpRequestMessage(method, path);
        if (body != null)
        {
            request.Content = JsonContent.Create(body);   // serializes the object to JSON text
        }

        // Send() blocks until the answer is back. Real clients usually use SendAsync (async/await comes later).
        using HttpResponseMessage response = _server!.Client.Send(request);
        string text = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();   // async/await comes later
        string? location = response.Headers.Location?.AbsolutePath;   // the port is random, so keep only the path

        Console.WriteLine($"{method.Method} {path}  ->  {(int)response.StatusCode} {response.StatusCode}");
        if (location != null)
        {
            Console.WriteLine($"  Location: {location}");
        }

        string? contentType = response.Content.Headers.ContentType?.MediaType;
        if (contentType == "application/problem+json")
        {
            PrintProblem(text);
        }
        else if (text.Length > 0)
        {
            Console.WriteLine($"  body: {text}");
        }

        return new ApiReply((int)response.StatusCode, text, location);
    }

    // A ProblemDetails body also carries a "type" link and a "traceId" that changes on every request; the
    // lesson prints the stable parts so its output is the same every run.
    private static void PrintProblem(string json)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        JsonElement root = document.RootElement;
        Console.WriteLine($"  problem: status {root.GetProperty("status").GetInt32()}, title \"{root.GetProperty("title").GetString()}\"");

        if (root.TryGetProperty("detail", out JsonElement detail))
        {
            Console.WriteLine($"  detail: {detail.GetString()}");
        }

        if (root.TryGetProperty("errors", out JsonElement errors))
        {
            foreach (JsonProperty field in errors.EnumerateObject().OrderBy(f => f.Name, StringComparer.Ordinal))
            {
                Console.WriteLine($"  error on {field.Name}: {string.Join(" / ", field.Value.EnumerateArray().Select(m => m.GetString()))}");
            }
        }
    }

    private static void PrintHeading(string title)
    {
        Console.WriteLine();
        Console.WriteLine($"=== {title} ===");
    }
}
