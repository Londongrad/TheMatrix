using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Matrix.BuildingBlocks.Api.HealthChecks
{
    internal sealed class InternalJwtRotationHealthCheck(
        string name,
        HealthStatus status,
        string description)
        : IHealthCheck
    {
        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            var result = new HealthCheckResult(
                status: status,
                description: $"{name}: {description}");

            return Task.FromResult(result);
        }
    }
}
