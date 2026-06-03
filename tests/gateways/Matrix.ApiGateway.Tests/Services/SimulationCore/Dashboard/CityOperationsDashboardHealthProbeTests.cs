using System.Net;
using Matrix.ApiGateway.Configurations.Options;
using Matrix.ApiGateway.Contracts.SimulationCore.Scenarios.ClassicCity.Dashboard;
using Matrix.ApiGateway.Services.SimulationCore.Dashboard;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Xunit;

namespace Matrix.ApiGateway.Tests.Services.SimulationCore.Dashboard
{
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
            var httpClientFactory = new RecordingHttpClientFactory((
                    _,
                    _) =>
                Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));
            CityOperationsDashboardHealthProbe probe = CreateProbe(
                gatewayHealth: HealthCheckResult.Healthy("ready"),
                httpClientFactory: httpClientFactory);

            IReadOnlyList<DashboardServiceHealthView> result = await probe.ProbeAsync(CancellationToken.None);

            Dictionary<string, DashboardServiceHealthView> byService = ToServiceDictionary(result);

            Assert.Equal(
                expected: 7,
                actual: result.Count);
            Assert.Contains(
                expected: "Gateway",
                collection: byService.Keys);
            Assert.Contains(
                expected: "SimulationCore",
                collection: byService.Keys);
            Assert.Contains(
                expected: "SimulationSystems",
                collection: byService.Keys);
            Assert.Contains(
                expected: "Resources",
                collection: byService.Keys);
            Assert.Contains(
                expected: "Population",
                collection: byService.Keys);
            Assert.Contains(
                expected: "Economy",
                collection: byService.Keys);
            Assert.Contains(
                expected: "Identity",
                collection: byService.Keys);
            Assert.All(
                collection: result,
                action: service =>
                {
                    Assert.Equal(
                        expected: "healthy",
                        actual: service.Status);
                    Assert.Equal(
                        expected: FixedNow,
                        actual: service.CheckedAtUtc);
                });
            Assert.Equal(
                expected: "Ready endpoint is healthy.",
                actual: byService["Gateway"].Detail);
            Assert.All(
                collection: result.Where(service => !string.Equals(
                    a: service.Service,
                    b: "Gateway",
                    comparisonType: StringComparison.Ordinal)),
                action: service => Assert.Equal(
                    expected: "Ready endpoint responded successfully.",
                    actual: service.Detail));
            Assert.All(
                collection: httpClientFactory.Requests,
                action: request =>
                {
                    Assert.Equal(
                        expected: HttpMethod.Get,
                        actual: request.Method);
                    Assert.Equal(
                        expected: "/health/ready",
                        actual: request.RequestUri?.AbsolutePath);
                });
        }

        [Fact]
        public async Task ProbeAsync_WhenDownstreamReadyEndpointReturnsNonSuccess_MarksServiceUnhealthy()
        {
            var httpClientFactory = new RecordingHttpClientFactory((
                request,
                _) =>
            {
                HttpStatusCode statusCode = string.Equals(
                    a: request.RequestUri?.Host,
                    b: "simulation-core.test",
                    comparisonType: StringComparison.Ordinal)
                    ? HttpStatusCode.ServiceUnavailable
                    : HttpStatusCode.OK;

                return Task.FromResult(new HttpResponseMessage(statusCode));
            });
            CityOperationsDashboardHealthProbe probe = CreateProbe(
                gatewayHealth: HealthCheckResult.Healthy("ready"),
                httpClientFactory: httpClientFactory);

            IReadOnlyList<DashboardServiceHealthView> result = await probe.ProbeAsync(CancellationToken.None);

            Dictionary<string, DashboardServiceHealthView> byService = ToServiceDictionary(result);

            Assert.Equal(
                expected: "unhealthy",
                actual: byService["SimulationCore"].Status);
            Assert.Equal(
                expected: "Ready endpoint responded with 503 ServiceUnavailable.",
                actual: byService["SimulationCore"].Detail);
            Assert.Equal(
                expected: FixedNow,
                actual: byService["SimulationCore"].CheckedAtUtc);
            Assert.Equal(
                expected: "healthy",
                actual: byService["Identity"].Status);
        }

        [Fact]
        public async Task ProbeAsync_WhenGatewayReadyCheckIsDegraded_ReturnsGatewayDegraded()
        {
            var httpClientFactory = new RecordingHttpClientFactory((
                    _,
                    _) =>
                Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));
            CityOperationsDashboardHealthProbe probe = CreateProbe(
                gatewayHealth: HealthCheckResult.Degraded("cache warming"),
                httpClientFactory: httpClientFactory);

            IReadOnlyList<DashboardServiceHealthView> result = await probe.ProbeAsync(CancellationToken.None);

            DashboardServiceHealthView gateway = ToServiceDictionary(result)["Gateway"];

            Assert.Equal(
                expected: "degraded",
                actual: gateway.Status);
            Assert.Contains(
                expectedSubstring: "cache warming",
                actualString: gateway.Detail,
                comparisonType: StringComparison.Ordinal);
            Assert.Equal(
                expected: FixedNow,
                actual: gateway.CheckedAtUtc);
        }

        private static CityOperationsDashboardHealthProbe CreateProbe(
            HealthCheckResult gatewayHealth,
            RecordingHttpClientFactory httpClientFactory)
        {
            return new CityOperationsDashboardHealthProbe(
                healthCheckService: CreateHealthCheckService(gatewayHealth),
                httpClientFactory: httpClientFactory,
                downstreamOptions: Options.Create(
                    new DownstreamServicesOptions
                    {
                        SimulationCore = "https://simulation-core.test",
                        SimulationSystems = "https://simulation-systems.test",
                        Resources = "https://resources.test",
                        Population = "https://population.test",
                        Economy = "https://economy.test",
                        Identity = "https://identity.test"
                    }),
                dashboardOptions: Options.Create(
                    new CityOperationsDashboardOptions
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
                return new HttpClient(
                    new RecordingHandler(async (
                        request,
                        cancellationToken) =>
                    {
                        lock (_syncRoot)
                            Requests.Add(request);

                        return await handler(
                            arg1: request,
                            arg2: cancellationToken);
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
                return handler(
                    arg1: request,
                    arg2: cancellationToken);
            }
        }
    }
}
