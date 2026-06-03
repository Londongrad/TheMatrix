using Matrix.ApiGateway.Configurations.Options;
using Matrix.ApiGateway.Contracts.SimulationCore.Scenarios.ClassicCity.Dashboard;
using Matrix.ApiGateway.Services.SimulationCore.Scenarios.ClassicCity.Dashboard;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Matrix.ApiGateway.Services.SimulationCore.Dashboard
{
    public sealed class CityOperationsDashboardHealthProbe(
        HealthCheckService healthCheckService,
        IHttpClientFactory httpClientFactory,
        IOptions<DownstreamServicesOptions> downstreamOptions,
        IOptions<CityOperationsDashboardOptions> dashboardOptions,
        TimeProvider timeProvider) : ICityOperationsDashboardHealthProbe
    {
        private readonly CityOperationsDashboardOptions _dashboardOptions = dashboardOptions.Value;
        private readonly DownstreamServicesOptions _downstreamOptions = downstreamOptions.Value;
        private readonly HealthCheckService _healthCheckService = healthCheckService;
        private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
        private readonly TimeProvider _timeProvider = timeProvider;

        public async Task<IReadOnlyList<DashboardServiceHealthView>> ProbeAsync(CancellationToken cancellationToken)
        {
            Task<DashboardServiceHealthView> gatewayTask = ProbeGatewayHealthAsync(cancellationToken);
            Task<DashboardServiceHealthView> simulationCoreTask = ProbeRemoteHealthAsync(
                service: "SimulationCore",
                baseUrl: _downstreamOptions.SimulationCore,
                cancellationToken: cancellationToken);
            Task<DashboardServiceHealthView> simulationSystemsTask = ProbeRemoteHealthAsync(
                service: "SimulationSystems",
                baseUrl: _downstreamOptions.SimulationSystems,
                cancellationToken: cancellationToken);
            Task<DashboardServiceHealthView> resourcesTask = ProbeRemoteHealthAsync(
                service: "Resources",
                baseUrl: _downstreamOptions.Resources,
                cancellationToken: cancellationToken);
            Task<DashboardServiceHealthView> populationTask = ProbeRemoteHealthAsync(
                service: "Population",
                baseUrl: _downstreamOptions.Population,
                cancellationToken: cancellationToken);
            Task<DashboardServiceHealthView> economyTask = ProbeRemoteHealthAsync(
                service: "Economy",
                baseUrl: _downstreamOptions.Economy,
                cancellationToken: cancellationToken);
            Task<DashboardServiceHealthView> identityTask = ProbeRemoteHealthAsync(
                service: "Identity",
                baseUrl: _downstreamOptions.Identity,
                cancellationToken: cancellationToken);

            return await Task.WhenAll(
                gatewayTask,
                simulationCoreTask,
                simulationSystemsTask,
                resourcesTask,
                populationTask,
                economyTask,
                identityTask);
        }

        private async Task<DashboardServiceHealthView> ProbeGatewayHealthAsync(CancellationToken cancellationToken)
        {
            DateTimeOffset checkedAt = _timeProvider.GetUtcNow();

            try
            {
                HealthReport report = await _healthCheckService.CheckHealthAsync(
                    predicate: registration => registration.Tags.Contains("ready"),
                    cancellationToken: cancellationToken);

                if (report.Status == HealthStatus.Healthy)
                    return new DashboardServiceHealthView(
                        Service: "Gateway",
                        Status: "healthy",
                        Detail: "Ready endpoint is healthy.",
                        CheckedAtUtc: checkedAt);

                string detail = report.Entries.Count == 0
                    ? "Gateway ready checks reported a non-healthy state."
                    : string.Join(
                        separator: " | ",
                        values: report.Entries.Select(entry =>
                            $"{entry.Key}: {(string.IsNullOrWhiteSpace(entry.Value.Description) ? entry.Value.Status.ToString() : entry.Value.Description)}"));

                return new DashboardServiceHealthView(
                    Service: "Gateway",
                    Status: report.Status == HealthStatus.Degraded
                        ? "degraded"
                        : "unhealthy",
                    Detail: detail,
                    CheckedAtUtc: checkedAt);
            }
            catch (Exception exception)
            {
                return new DashboardServiceHealthView(
                    Service: "Gateway",
                    Status: "unhealthy",
                    Detail: $"Gateway health probe failed: {exception.GetType().Name}.",
                    CheckedAtUtc: checkedAt);
            }
        }

        private async Task<DashboardServiceHealthView> ProbeRemoteHealthAsync(
            string service,
            string baseUrl,
            CancellationToken cancellationToken)
        {
            DateTimeOffset checkedAt = _timeProvider.GetUtcNow();

            try
            {
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(TimeSpan.FromSeconds(_dashboardOptions.HealthProbeTimeoutSeconds));

                HttpClient client = _httpClientFactory.CreateClient();
                client.BaseAddress = new Uri(
                    uriString: baseUrl,
                    uriKind: UriKind.Absolute);

                using HttpResponseMessage response = await client.GetAsync(
                    requestUri: "/health/ready",
                    cancellationToken: timeoutCts.Token);

                return response.IsSuccessStatusCode
                    ? new DashboardServiceHealthView(
                        Service: service,
                        Status: "healthy",
                        Detail: "Ready endpoint responded successfully.",
                        CheckedAtUtc: checkedAt)
                    : new DashboardServiceHealthView(
                        Service: service,
                        Status: "unhealthy",
                        Detail: $"Ready endpoint responded with {(int)response.StatusCode} {response.StatusCode}.",
                        CheckedAtUtc: checkedAt);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                return new DashboardServiceHealthView(
                    Service: service,
                    Status: "degraded",
                    Detail: "Ready probe timed out.",
                    CheckedAtUtc: checkedAt);
            }
            catch (Exception exception)
            {
                return new DashboardServiceHealthView(
                    Service: service,
                    Status: "unhealthy",
                    Detail: $"Ready probe failed: {exception.GetType().Name}.",
                    CheckedAtUtc: checkedAt);
            }
        }
    }
}
