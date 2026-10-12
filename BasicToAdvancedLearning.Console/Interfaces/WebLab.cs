using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BasicToAdvancedLearning.Interfaces;

// Shared plumbing for the Web API topics (16+). This folder holds shared code, not a lesson.
//
// A real Web API is its own program that runs forever. To keep every lesson a console program that
// finishes, WebLab starts a small web server INSIDE this process, on a free local port (nothing is
// reachable from outside your machine), lets the lesson send real HTTP requests to it with HttpClient,
// then stops it. What the server does is exactly what a deployed API does; only the hosting is shortcut.
internal static class WebLab
{
    // Creates a web app listening on http://127.0.0.1:<free port>. The caller registers services on
    // builder.Services, then calls Build(), maps endpoints, and calls Run(). Use Start() for the usual case.
    public static WebApplicationBuilder CreateBuilder()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            ContentRootPath = AppContext.BaseDirectory,   // do not scan the folder the user ran from
            EnvironmentName = Environments.Development,
        });
        builder.WebHost.UseUrls("http://127.0.0.1:0");   // port 0 = "any free port"
        builder.Logging.ClearProviders();                 // keep the lesson's console output readable
        return builder;
    }

    // Registers MVC controllers, but ONLY the listed controller classes (each topic has its own, and the
    // default would discover every controller in the whole program).
    public static IMvcBuilder AddControllersOnly(this IServiceCollection services, params Type[] controllers)
    {
        IMvcBuilder mvc = services.AddControllers();
        mvc.ConfigureApplicationPartManager(manager =>
        {
            manager.ApplicationParts.Clear();
            manager.ApplicationParts.Add(new TypesPart(controllers));
        });
        return mvc;
    }

    // Starts the app (non-blocking) and returns a running server plus an HttpClient pointed at it.
    public static RunningServer Start(WebApplication app)
    {
        app.Start();
        string address = app.Urls.First();
        return new RunningServer(app, address);
    }

    private sealed class TypesPart : ApplicationPart, IApplicationPartTypeProvider
    {
        private readonly TypeInfo[] _types;
        public TypesPart(IEnumerable<Type> types) => _types = types.Select(t => t.GetTypeInfo()).ToArray();
        public override string Name => "WebLab.Controllers";
        public IEnumerable<TypeInfo> Types => _types;
    }
}

internal sealed class RunningServer : IDisposable
{
    private readonly WebApplication _app;

    public RunningServer(WebApplication app, string baseAddress)
    {
        _app = app;
        BaseAddress = baseAddress;
        Client = new HttpClient { BaseAddress = new Uri(baseAddress) };
    }

    public string BaseAddress { get; }
    public HttpClient Client { get; }

    public void Dispose()
    {
        Client.Dispose();
        _app.StopAsync().GetAwaiter().GetResult();   // async/await comes later in the course
        _app.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }
}
