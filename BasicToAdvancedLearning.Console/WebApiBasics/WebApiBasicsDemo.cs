using System.Text;
using System.Text.Json;
using BasicToAdvancedLearning.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace BasicToAdvancedLearning.WebApiBasics;

// Topic 17: ASP.NET Core Web API basics - controllers, routing, model binding, action results and
// dependency injection. The running example is the bank: accounts, kept in memory (no database here).
//
// A real Web API is its own program that keeps running and waits for requests. To let this console lesson
// finish, WebLab (Interfaces/WebLab.cs) starts the same kind of server INSIDE this program, on a free local
// port, and the lesson talks to it with HttpClient, exactly like any other client would. The port is
// random, so the output only shows the path, such as "GET /api/accounts".
// JSON in the output is ASP.NET Core's default: property names start with a lowercase letter (accountId).
// See ArchitectureDiagrams/WebApiBasics for the visual walkthrough.
public class WebApiBasicsDemo
{
    // JSON that stops in the middle, as if the request was cut off or badly typed. Kept as a constant
    // so the method that uses it stays easy to read.
    private const string UnfinishedJson = "{\"amount\": ";

    // 1. One controller, one action: how a request finds the code that answers it.
    public void ExplainControllerAndAction()
    {
        PrintHeading("1. Controller and action - how a request finds your code");

        // Four steps every ASP.NET Core API goes through (this is what the web template does for you):
        WebApplicationBuilder builder = WebLab.CreateBuilder();            // 1) create the app builder
        builder.Services.AddControllersOnly(typeof(AccountsController));   // 2) register the controller classes
        WebApplication app = builder.Build();                              // 3) build the app
        app.MapControllers();                                              // 4) turn [Route]/[HttpGet] into endpoints

        using RunningServer server = WebLab.Start(app);

        // The request goes to /api/accounts. The router (matching URL + HTTP method) finds
        // [Route("api/[controller]")] on AccountsController plus [HttpGet] on GetAccountNumbers,
        // so that action runs and its return value becomes the JSON body.
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/accounts");
        using HttpResponseMessage response = server.Client.Send(request);   // blocking call; async/await comes later
        string body = ReadBody(response);
        Console.WriteLine($"GET /api/accounts -> {FormatStatus(response)}  body: {body}");

        // What [ApiController] adds, each shown later in this lesson:
        //   - a failed binding or validation is answered with 400 automatically (method 3);
        //   - a class-type parameter is read from the JSON body without writing [FromBody] (method 3);
        //   - error statuses such as 404 get a standard JSON "ProblemDetails" body (method 4).
        // Upper/lower case in the path does not matter, but the HTTP method does: the route exists for GET only,
        // so a POST to the same path is answered 405 Method Not Allowed.
        Call(server.Client, HttpMethod.Get, "/API/ACCOUNTS");
        Call(server.Client, HttpMethod.Post, "/api/accounts");
    }

    // 2. Routing: URL templates, route parameters, constraints, and what a non-match looks like.
    public void ExplainRouting()
    {
        PrintHeading("2. Routing - URL templates, parameters and constraints");

        WebApplicationBuilder builder = WebLab.CreateBuilder();
        builder.Services.AddControllersOnly(typeof(RoutedAccountsController));
        using RunningServer server = StartApi(builder);

        // {id:int:min(1)} captures the segment into `id` - but only when it is an int that is at least 1.
        Call(server.Client, HttpMethod.Get, "/api/accounts/7");
        Call(server.Client, HttpMethod.Get, "/api/accounts/by-number/AC-1001");

        // When the constraint fails, this route is simply "not a match". No other route fits either, so the
        // router answers 404 (it does not say WHICH part failed). An unknown path gets the same 404.
        Call(server.Client, HttpMethod.Get, "/api/accounts/abc");
        Call(server.Client, HttpMethod.Get, "/api/accounts/0");
        Call(server.Client, HttpMethod.Get, "/api/customers");

        // One URL + HTTP method must lead to ONE action. ClashController has two actions with the same
        // route, so the framework cannot choose: the request fails with 500 (an AmbiguousMatchException).
        // Only the exception's type is printed; this lesson's server runs in the Development environment,
        // which shows exception details to the caller, and a deployed API should not.
        ShowRouteClash();
    }

    // 3. Model binding: filling action parameters from the route, the query string and the JSON body.
    public void ExplainModelBinding()
    {
        PrintHeading("3. Model binding - route, query string and JSON body");

        WebApplicationBuilder builder = WebLab.CreateBuilder();
        builder.Services.AddControllersOnly(typeof(BindingController), typeof(PlainBindingController));
        using RunningServer server = StartApi(builder);

        // The same action, filled from different places: id from the route, last from the query string.
        Call(server.Client, HttpMethod.Get, "/api/accounts/3/statement?last=2");
        Call(server.Client, HttpMethod.Get, "/api/accounts/3/statement");   // ?last missing -> default value 5

        // A JSON body becomes a DepositRequest object ("amount" matches the property Amount; case is ignored).
        Call(server.Client, HttpMethod.Post, "/api/accounts/3/deposits", "{\"amount\": 250.50}");

        // Binding goes wrong: [ApiController] answers 400 by itself and the action never runs.
        Call(server.Client, HttpMethod.Get, "/api/accounts/3/statement?last=abc");   // not a number
        Call(server.Client, HttpMethod.Post, "/api/accounts/3/deposits", UnfinishedJson);   // broken JSON
        Call(server.Client, HttpMethod.Post, "/api/accounts/3/deposits", "{\"amount\": -5}");  // breaks [Range]

        // The same kind of requests against actions WITHOUT [ApiController] (checked when this lesson was
        // written): the bad ?last=abc is quietly ignored (the default 5 is used) and the JSON body is not read
        // for a class-type parameter, so the deposit arrives as 0. [ApiController] is what protects you.
        Call(server.Client, HttpMethod.Get, "/api/plain/3/statement?last=abc");
        Call(server.Client, HttpMethod.Post, "/api/plain/3/deposits", "{\"amount\": 250.50}");
    }

    // 4. Action results: the status code is chosen by what the action returns.
    public void ExplainActionResults()
    {
        PrintHeading("4. Action results - return the object (200), NotFound, CreatedAtAction, BadRequest, NoContent");

        ActionResultsController.ResetData();
        WebApplicationBuilder builder = WebLab.CreateBuilder();
        builder.Services.AddControllersOnly(typeof(ActionResultsController));
        using RunningServer server = StartApi(builder);

        // GetById returns the object -> 200. When it returns NotFound(), the [ApiController] attribute fills in
        // a ProblemDetails body; compare with method 2, where the ROUTER's 404 had an empty body.
        Call(server.Client, HttpMethod.Get, "/api/accounts/1");        // returns the object -> 200
        Call(server.Client, HttpMethod.Get, "/api/accounts/99");       // NotFound() -> 404
        Call(server.Client, HttpMethod.Post, "/api/accounts", NewAccountJson("AC-1003", "Savings"));   // CreatedAtAction -> 201
        Call(server.Client, HttpMethod.Post, "/api/accounts", NewAccountJson("AC-1004", "Gold"));      // BadRequest -> 400
        Call(server.Client, HttpMethod.Delete, "/api/accounts/3");     // NoContent() -> 204
        Call(server.Client, HttpMethod.Get, "/api/accounts/3");        // gone -> 404
    }

    // 5. Dependency injection: the controller asks for an IAccountStore; the container supplies one.
    public void ExplainDependencyInjection()
    {
        PrintHeading("5. Dependency injection - the controller asks, the container supplies");

        WebApplicationBuilder builder = WebLab.CreateBuilder();
        builder.Services.AddControllersOnly(typeof(StoreAccountsController));

        // Registration: "when someone asks for IAccountStore, give them an InMemoryAccountStore".
        // AddSingleton = ONE store for the whole life of the app, shared by every request. Watch the
        // "[server]" lines: the store is created the first time it is needed, and then reused. The controller is
        // created again for every request and never calls `new` on a store itself.
        builder.Services.AddSingleton<IAccountStore, InMemoryAccountStore>();
        using RunningServer server = StartApi(builder);

        Console.WriteLine("Registered with AddSingleton:");
        RunStoreScenario(server.Client);

        // Same controller, different registration: only the lifetime changed. AddScoped = a new store for
        // every request, so what the POST added is gone when the next request arrives.
        Console.WriteLine("Registered with AddScoped (one store per request):");
        ShowScopedStore();
    }

    // 6. Service lifetimes: Singleton, Scoped and Transient, told apart by creation numbers.
    public void ExplainServiceLifetimes()
    {
        PrintHeading("6. Service lifetimes - Singleton, Scoped, Transient");

        Probe.ResetCounter();
        WebApplicationBuilder builder = WebLab.CreateBuilder();
        builder.Services.AddControllersOnly(typeof(LifetimesController));
        builder.Services.AddSingleton<SingletonProbe>();   // created once, reused by everyone
        builder.Services.AddScoped<ScopedProbe>();          // created once per request
        builder.Services.AddTransient<TransientProbe>();    // created every time someone asks
        builder.Services.AddTransient<ProbeReporter>();
        using RunningServer server = StartApi(builder);

        // Each probe has a creation number. Both the controller and the ProbeReporter ask for all three.
        // Read the numbers: the singleton keeps #1 across both requests; the scoped probe is shared by the
        // controller and the reporter inside one request but is new in the next request; the transient probe
        // is a different object every time it is asked for, even twice inside one request.
        Call(server.Client, HttpMethod.Get, "/api/lifetimes");
        Call(server.Client, HttpMethod.Get, "/api/lifetimes");
    }

    // ---- Lesson plumbing (not part of what is being taught) ----

    // Builds the app, maps the controllers' routes, and starts it (steps 3 and 4 of method 1, plus Start).
    private static RunningServer StartApi(WebApplicationBuilder builder)
    {
        WebApplication app = builder.Build();
        app.MapControllers();
        return WebLab.Start(app);
    }

    // GET the list, POST one new account, GET the list again.
    private static void RunStoreScenario(HttpClient client)
    {
        Call(client, HttpMethod.Get, "/api/accounts");
        Call(client, HttpMethod.Post, "/api/accounts", NewAccountJson("AC-1003", "Savings"));
        Call(client, HttpMethod.Get, "/api/accounts");
    }

    // The same API with the store registered as Scoped.
    private static void ShowScopedStore()
    {
        WebApplicationBuilder builder = WebLab.CreateBuilder();
        builder.Services.AddControllersOnly(typeof(StoreAccountsController));
        builder.Services.AddScoped<IAccountStore, InMemoryAccountStore>();
        using RunningServer server = StartApi(builder);
        RunStoreScenario(server.Client);
    }

    private static void ShowRouteClash()
    {
        WebApplicationBuilder builder = WebLab.CreateBuilder();
        builder.Services.AddControllersOnly(typeof(ClashController));
        using RunningServer server = StartApi(builder);
        Call(server.Client, HttpMethod.Get, "/api/clash");
    }

    // Sends one request, waits for the answer, and prints a one-line summary of it.
    private static void Call(HttpClient client, HttpMethod method, string path, string? json = null)
    {
        using var request = new HttpRequestMessage(method, path);
        if (json != null)
        {
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }
        using HttpResponseMessage response = client.Send(request);   // blocking call; async/await comes later
        string line = $"{method} {path} -> {FormatStatus(response)}  {Summarise(response)}";
        Console.WriteLine(line);
    }

    private static string FormatStatus(HttpResponseMessage response) => $"{(int)response.StatusCode} {response.StatusCode}";

    private static string ReadBody(HttpResponseMessage response) => response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

    // Turns a response into a short, repeatable description. Error bodies from the framework carry a
    // changing "traceId" and wording that differs between versions, so those are reduced to their stable parts.
    private static string Summarise(HttpResponseMessage response)
    {
        string body = ReadBody(response);
        string location = "";
        if (response.Headers.Location != null)
        {
            Uri uri = response.Headers.Location;
            location = $"Location: {(uri.IsAbsoluteUri ? uri.AbsolutePath : uri.OriginalString)}  ";
        }

        if (body.Length == 0)
        {
            return location + "(empty body)";
        }
        if (response.Content.Headers.ContentType?.MediaType == "application/problem+json")
        {
            return location + SummariseProblemDetails(body);
        }
        if (response.StatusCode == System.Net.HttpStatusCode.InternalServerError)
        {
            // The server threw an exception. Show only its type name (the text before the first ':').
            return location + $"error: {body.Split(':')[0]}";
        }
        if (body.StartsWith("[{"))
        {
            return location + SummariseAccountList(body);
        }
        return location + $"body: {body}";
    }

    // A list of accounts is long; print how many there are and their numbers.
    private static string SummariseAccountList(string body)
    {
        List<Account> accounts = JsonSerializer.Deserialize<List<Account>>(body, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        return $"{accounts.Count} accounts: {string.Join(", ", accounts.Select(a => a.AccountNumber))}";
    }

    // "ProblemDetails" is the standard JSON shape for an HTTP error: a status, a title, and for binding
    // problems an "errors" entry. Only the facts that stay the same between ASP.NET Core versions are printed:
    // the key names and messages inside "errors" (for example for a broken JSON body) can differ.
    private static string SummariseProblemDetails(string body)
    {
        using JsonDocument json = JsonDocument.Parse(body);
        JsonElement root = json.RootElement;
        string text = $"ProblemDetails status={root.GetProperty("status").GetInt32()}";
        if (root.TryGetProperty("errors", out JsonElement errors))
        {
            text += ", with an errors entry";
        }
        return text;
    }

    // The JSON body for opening an account.
    private static string NewAccountJson(string number, string type) => $"{{\"accountNumber\":\"{number}\",\"accountType\":\"{type}\"}}";

    private static void PrintHeading(string title)
    {
        Console.WriteLine();
        Console.WriteLine(title);
    }
}
