using System.Net;
using Matrix.ApiGateway.Configurations.Options;
using Matrix.ApiGateway.Contracts.SimulationCore.Dashboard;
using Matrix.ApiGateway.Services.SimulationCore.Dashboard;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Xunit;

namespace Matrix.ApiGateway.Tests.Services.SimulationCore.Dashboard;

public sealed class CityOperationsDashboardHealthProbeTests
{
    private static readonly DateTimeOffset FixedNow = new(
        year: 2048,
        month: 6,
        day: 1,
        hour: 12,
        minute: 0,
        second: 0,
        offset: TimeSpan.Zero);

    [Fact]
    public async Task ProbeAsync_WhenGatewayAndDownstreamsAreHealthy_ReturnsHealthyServicesWithFixedTimestamp()
    {
        var httpClientFactory = new RecordingHttpClientFactory((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));
        CityOperationsDashboardHealthProbe probe = CreateProbe(
            gatewayHealth: HealthCheckResult.Healthy("ready"),
            httpClientFactory: httpClientFactory);

        IReadOnlyList<DashboardServiceHealthView> result = await probe.ProbeAsync(CancellationToken.None);

        Dictionary<string, DashboardServiceHealthView> byService = ToServiceDictionary(result);

        Assert.Equal(7, result.Count);
        Assert.Contains("Gateway", byService.Keys);
        Assert.Contains("SimulationCore", byService.Keys);
        Assert.Contains("SimulationSystems", byService.Keys);
        Assert.Contains("Resources", byService.Keys);
        Assert.Contains("Population", byService.Keys);
        Assert.Contains("Economy", byService.Keys);
        Assert.Contains("Identity", byService.Keys);
        Assert.All(result, service =>
        {
            Assert.Equal("healthy", service.Status);
            Assert.Equal(FixedNow, service.CheckedAtUtc);
        });
        Assert.Equal("Ready endpoint is healthy.", byService["Gateway"].Detail);
        Assert.All(
            result.Where(service => !string.Equals(service.Service, "Gateway", StringComparison.Ordinal)),
            service => Assert.Equal("Ready endpoint responded successfully.", service.Detail));
        Assert.All(httpClientFactory.Requests, request =>
        {
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal("/health/ready", request.RequestUri?.AbsolutePath);
        });
    }

    [Fact]
    public async Task ProbeAsync_WhenDownstreamReadyEndpointReturnsNonSuccess_MarksServiceUnhealthy()
    {
        var httpClientFactory = new RecordingHttpClientFactory((request, _) =>
        {
            HttpStatusCode statusCode = string.Equals(
                request.RequestUri?.Host,
                "simulation-core.test",
                StringComparison.Ordinal)
                ? HttpStatusCode.ServiceUnavailable
                : HttpStatusCode.OK;

            return Task.FromResult(new HttpResponseMessage(statusCode));
        });
        CityOperationsDashboardHealthProbe probe = CreateProbe(
            gatewayHealth: HealthCheckResult.Healthy("ready"),
            httpClientFactory: httpClientFactory);

        IReadOnlyList<DashboardServiceHealthView> result = await probe.ProbeAsync(CancellationToken.None);

        Dictionary<string, DashboardServiceHealthView> byService = ToServiceDictionary(result);

        Assert.Equal("unhealthy", byService["SimulationCore"].Status);
        Assert.Equal(
            "Ready endpoint responded with 503 ServiceUnavailable.",
            byService["SimulationCore"].Detail);
        Assert.Equal(FixedNow, byService["SimulationCore"].CheckedAtUtc);
        Assert.Equal("healthy", byService["Identity"].Status);
    }

    [Fact]
    public async Task ProbeAsync_WhenGatewayReadyCheckIsDegraded_ReturnsGatewayDegraded()
    {
        var httpClientFactory = new RecordingHttpClientFactory((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));
        CityOperationsDashboardHealthProbe probe = CreateProbe(
            gatewayHealth: HealthCheckResult.Degraded("cache warming"),
            httpClientFactory: httpClientFactory);

        IReadOnlyList<DashboardServiceHealthView> result = await probe.ProbeAsync(CancellationToken.None);

        DashboardServiceHealthView gateway = ToServiceDictionary(result)["Gateway"];

        Assert.Equal("degraded", gateway.Status);
        Assert.Contains("cache warming", gateway.Detail, StringComparison.Ordinal);
        Assert.Equal(FixedNow, gateway.CheckedAtUtc);
    }

    private static CityOperationsDashboardHealthProbe CreateProbe(
        HealthCheckResult gatewayHealth,
        RecordingHttpClientFactory httpClientFactory)
    {
        return new CityOperationsDashboardHealthProbe(
            healthCheckService: CreateHealthCheckService(gatewayHealth),
            httpClientFactory: httpClientFactory,
            downstreamOptions: Options.Create(new DownstreamServicesOptions
            {
                SimulationCore = "https://simulation-core.test",
                SimulationSystems = "https://simulation-systems.test",
                Resources = "https://resources.test",
                Population = "https://population.test",
                Economy = "https://economy.test",
                Identity = "https://identity.test"
            }),
            dashboardOptions: Options.Create(new CityOperationsDashboardOptions
            {
                HealthProbeTimeoutSeconds = 5,
                PanelReadTimeoutSeconds = 4,
                MaxConcurrentCitySnapshotLoads = 4
            }),
            timeProvider: new FixedTimeProvider(FixedNow));
    }

    private static HealthCheckService CreateHealthCheckService(HealthCheckResult result)
    {
        var services = new ServiceCollection();

        services.AddLogging();
        services
           .AddHealthChecks()
           .AddCheck(
                name: "ready",
                check: () => result,
                tags: ["ready"]);

        return services
           .BuildServiceProvider()
           .GetRequiredService<HealthCheckService>();
    }

    private static Dictionary<string, DashboardServiceHealthView> ToServiceDictionary(
        IReadOnlyList<DashboardServiceHealthView> services)
    {
        return services.ToDictionary(
            keySelector: service => service.Service,
            elementSelector: service => service,
            comparer: StringComparer.Ordinal);
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow()
        {
            return utcNow;
        }
    }

    private sealed class RecordingHttpClientFactory(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler) : IHttpClientFactory
    {
        private readonly object _syncRoot = new();

        public List<HttpRequestMessage> Requests { get; } = [];

        public HttpClient CreateClient(string name)
        {
            return new HttpClient(new RecordingHandler(async (request, cancellationToken) =>
            {
                lock (_syncRoot)
                {
                    Requests.Add(request);
                }

                return await handler(request, cancellationToken);
            }));
        }
    }

    private sealed class RecordingHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return handler(request, cancellationToken);
        }
    }
}
