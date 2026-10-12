using Microsoft.Extensions.Diagnostics.HealthChecks;
using FullStackBank.Api.Data;

namespace FullStackBank.Api.Infrastructure;

// Behind GET /health. "The process is up" is not enough for a load balancer or a deploy script: an API that
// cannot reach its database cannot do its job, so /health also tries to open the database.
public class DatabaseHealthCheck : IHealthCheck
{
    private readonly BankDbContext _db;

    public DatabaseHealthCheck(BankDbContext db)
    {
        _db = db;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        bool canConnect = await _db.Database.CanConnectAsync(cancellationToken);
        return canConnect
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy("Cannot connect to the database.");
    }
}
