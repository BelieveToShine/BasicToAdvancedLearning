using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using BasicToAdvancedLearning.Interfaces;

namespace BasicToAdvancedLearning.JwtAuth;

// Starts the pretend bank API inside this console program (a real API is a separate program that keeps
// running; WebLab hosts it in-process only so the lesson can finish) with JWT checking switched on.
// Every server gets its OWN new signing key, so a token from one server is not accepted by another.
internal sealed class DemoAuthServer : IDisposable
{
    private readonly RunningServer _running;

    private DemoAuthServer(RunningServer running, TokenIssuer issuer)
    {
        _running = running;
        Issuer = issuer;
    }

    public HttpClient Client => _running.Client;
    public TokenIssuer Issuer { get; }

    // clockSkew = how much past the exp time the server still accepts a token (see TokenLifetime in the lesson).
    // Passing null leaves the library's own default in place.
    public static DemoAuthServer Start(TimeSpan? clockSkew)
    {
        var issuer = new TokenIssuer();
        WebApplicationBuilder builder = WebLab.CreateBuilder();
        builder.Services.AddSingleton(issuer);
        builder.Services.AddSingleton(new DemoUsers());   // the in-memory users
        builder.Services.AddControllersOnly(typeof(AuthDemoController));

        // Authentication = "read the Authorization header and work out who this is".
        builder.Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                // Keep claim names exactly as written in the token. If you leave this at its default (true), some claim
                // names are rewritten to long URIs (checked by running: "role" became a long name, so
                // User.FindFirst("role") found nothing and the lesson's greeting printed an empty role).
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,    // the signature must match our key
                    IssuerSigningKey = issuer.Key,
                    ValidateIssuer = true,
                    ValidIssuer = TokenIssuer.Issuer,
                    ValidateAudience = true,
                    ValidAudience = TokenIssuer.Audience,
                    ValidateLifetime = true,            // reject tokens past exp (or before nbf)
                    NameClaimType = "name",             // which claim User.Identity.Name reads
                    RoleClaimType = "role",             // which claim [Authorize(Roles = ...)] checks
                };
                if (clockSkew.HasValue)
                {
                    options.TokenValidationParameters.ClockSkew = clockSkew.Value;
                }
            });
        // Authorization = "given who this is, are they allowed in here?"
        builder.Services.AddAuthorization();

        WebApplication app = builder.Build();
        app.UseAuthentication();   // order matters: first find out WHO ...
        app.UseAuthorization();    // ... then decide whether that is ALLOWED
        app.MapControllers();
        return new DemoAuthServer(WebLab.Start(app), issuer);
    }

    public void Dispose() => _running.Dispose();
}
