using FullStackBank.Api.Data;
using FullStackBank.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

// FullStackBank.Api - the "real" bank API. It is a separate program from the console lessons: a deployed
// API keeps running on its own, and the React front end (frontend/) talks to it over HTTP.

var builder = WebApplication.CreateBuilder(args);

// Listen on http://localhost:5080 (the Vite dev-server proxy in frontend/vite.config.js points here).
// Properties/launchSettings.json sets the same URL for `dotnet run`; this line is the fallback when the API is
// started another way. An explicit --urls / ASPNETCORE_URLS still wins, so you can pick another port.
if (string.IsNullOrEmpty(builder.Configuration["urls"]))
{
    builder.WebHost.UseUrls("http://localhost:5080");
}

// JSON is camelCase by default in ASP.NET Core web apps. The automatic 400 for invalid requests is kept, only
// its error keys are camelCased (see ValidationErrors).
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(o => o.InvalidModelStateResponseFactory = ValidationErrors.CamelCaseResponse);

// ProblemDetails (RFC 9457) = the standard JSON error shape { type, title, status, detail, traceId }.
// AddProblemDetails makes the framework use it for errors without a body too (for example an unknown URL -> 404).
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();

// AddDbContext registers BankDbContext with a per-request lifetime. The connection string is read lazily from
// BTAL_SQLSERVER (see BankDatabase); it is never in source or appsettings.
builder.Services.AddDbContext<BankDbContext>(options => options.UseSqlServer(BankDatabase.ConnectionString));

builder.Services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("database");

var app = builder.Build();

// Fail fast, with a clear one-line message, if the connection string is missing (instead of on the first request).
try
{
    _ = BankDatabase.ConnectionString;
}
catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
{
    // InvalidOperationException = variable missing; ArgumentException = the value is not a valid connection string.
    Console.Error.WriteLine(ex is ArgumentException
        ? $"The {BankDatabase.EnvironmentVariable} value is not a valid SQL Server connection string: {ex.Message}"
        : ex.Message);
    return 1;
}

if (app.Environment.IsDevelopment())
{
    // Development convenience ONLY: create the database, apply migrations, add sample data.
    // In production you do NOT do this at startup, because:
    //  * the app's database login should not be allowed to change the schema;
    //  * when several copies of the API start at once they would all try to migrate the same database;
    //  * a failed or slow migration would stop the API from starting, and nobody reviewed the SQL first.
    // Instead you generate a script (dotnet ef migrations script --idempotent) and run it as a deployment step.
    try
    {
        await BankDatabase.EnsureDatabaseExistsAsync();
        using IServiceScope scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BankDbContext>();
        await db.Database.MigrateAsync();
        await BankDatabase.SeedIfEmptyAsync(db);
    }
    catch (Microsoft.Data.SqlClient.SqlException ex)
    {
        // Server down, wrong password, no right to create the database... a one-line message instead of a stack trace.
        Console.Error.WriteLine($"Could not prepare the database (SQL Server error {ex.Number}): {ex.Message.Split('\n')[0].Trim()}");
        Console.Error.WriteLine($"Check the {BankDatabase.EnvironmentVariable} value and that SQL Server is running.");
        return 1;
    }
}

// Order matters: the exception handler goes FIRST so it wraps everything after it. It is NOT limited to
// Development (we deliberately do not use the developer exception page, which shows stack traces).
app.UseExceptionHandler();
// Turns bodyless error answers (404 for an unknown URL, 405...) into ProblemDetails too.
app.UseStatusCodePages();

app.MapControllers();
app.MapHealthChecks("/health");   // 200 "Healthy" or 503 "Unhealthy" (text/plain)

app.Run();

return 0;
