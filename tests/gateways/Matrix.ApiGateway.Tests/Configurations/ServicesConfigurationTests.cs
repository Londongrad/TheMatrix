using System.Net;
using Matrix.ApiGateway.Authorization.InternalJwt;
using Matrix.ApiGateway.Authorization.InternalJwt.Abstractions;
using Matrix.ApiGateway.Configurations;
using Matrix.ApiGateway.Configurations.Options;
using Matrix.ApiGateway.Configurations.Security;
using Matrix.ApiGateway.DownstreamClients.Economy;
using Matrix.ApiGateway.DownstreamClients.Identity.Self.Auth;
using Matrix.ApiGateway.DownstreamClients.Population.People;
using Matrix.ApiGateway.DownstreamClients.SimulationCore.Scenarios.ClassicCity.Cities;
using Matrix.ApiGateway.Services.SimulationCore.Scenarios.ClassicCity.Cities;
using Matrix.ApiGateway.Services.SimulationCore.Scenarios.ClassicCity.Dashboard;
using Matrix.BuildingBlocks.Api.Forwarding;
using Matrix.BuildingBlocks.Application.Authorization.Jwt;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using Xunit;
using static Matrix.ApiGateway.Tests.TestSupport.StartupTestSupport;

namespace Matrix.ApiGateway.Tests.Configurations
{
    public sealed class ServicesConfigurationTests
    {
        [Fact]
        public void ConfigureApplicationServices_WhenRedisConnectionStringIsMissing_ThrowsWhenMultiplexerIsResolved()
        {
            IConfiguration configuration = OverrideConfiguration(("Redis:ConnectionString", ""));
            WebApplicationBuilder builder = CreateBuilder(configuration);

            builder.ConfigureApplicationServices();

            using ServiceProvider provider = builder.Services.BuildServiceProvider();

            InvalidOperationException exception =
                Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<IConnectionMultiplexer>());

            Assert.Contains(
                expectedSubstring: "Redis:ConnectionString is required for gateway session orchestration.",
                actualString: exception.Message);
        }

        [Fact]
        public void ConfigureApplicationServices_WhenConfigurationIsValid_RegistersGatewayServicesAuthAndTypedClients()
        {
            WebApplicationBuilder builder = CreateBuilder(BuildValidApiConfiguration());

            builder.ConfigureApplicationServices();

            using ServiceProvider provider = builder.Services.BuildServiceProvider();
            using IServiceScope scope = provider.CreateScope();

            AuthenticationOptions authentication = provider.GetRequiredService<IOptions<AuthenticationOptions>>()
               .Value;
            DownstreamServicesOptions downstreamServices =
                provider.GetRequiredService<IOptions<DownstreamServicesOptions>>()
                   .Value;
            CityOperationsDashboardOptions dashboardOptions =
                provider.GetRequiredService<IOptions<CityOperationsDashboardOptions>>()
                   .Value;
            FrontendSecurityOptions frontendSecurity =
                provider.GetRequiredService<IOptions<FrontendSecurityOptions>>()
                   .Value;
            TrustedForwardedHeadersOptions trustedForwarding =
                provider.GetRequiredService<IOptions<TrustedForwardedHeadersOptions>>()
                   .Value;
            ForwardedHeadersOptions forwardedHeaders =
                provider.GetRequiredService<IOptions<ForwardedHeadersOptions>>()
                   .Value;
            InternalUserContextJwtOptions internalJwt =
                provider.GetRequiredService<IOptions<InternalUserContextJwtOptions>>()
                   .Value;

            Assert.Equal(
                expected: JwtBearerDefaults.AuthenticationScheme,
                actual: authentication.DefaultAuthenticateScheme);
            Assert.Equal(
                expected: JwtBearerDefaults.AuthenticationScheme,
                actual: authentication.DefaultChallengeScheme);
            Assert.NotNull(scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>());
            Assert.NotNull(scope.ServiceProvider.GetRequiredService<IInternalJwtIssuer>());
            Assert.NotNull(scope.ServiceProvider.GetRequiredService<IInternalJwtRequestContextAccessor>());
            Assert.NotNull(scope.ServiceProvider.GetRequiredService<ICityOperationsDashboardHealthProbe>());
            Assert.NotNull(scope.ServiceProvider.GetRequiredService<ICityOperationsDashboardSnapshotLoader>());
            Assert.NotNull(scope.ServiceProvider.GetRequiredService<ICityOperationsDashboardAlertBuilder>());
            Assert.NotNull(scope.ServiceProvider.GetRequiredService<ICityOperationsDashboardRecentEventsBuilder>());
            Assert.NotNull(scope.ServiceProvider.GetRequiredService<ICityOperationsDashboardService>());
            Assert.NotNull(scope.ServiceProvider.GetRequiredService<ICityProvisioningService>());
            Assert.NotNull(scope.ServiceProvider.GetRequiredService<ICitiesApiClient>());
            Assert.NotNull(scope.ServiceProvider.GetRequiredService<IPopulationApiClient>());
            Assert.NotNull(scope.ServiceProvider.GetRequiredService<IEconomyApiClient>());
            Assert.NotNull(scope.ServiceProvider.GetRequiredService<IIdentityAuthClient>());
            Assert.Same(
                expected: TimeProvider.System,
                actual: provider.GetRequiredService<TimeProvider>());

            Assert.Equal(
                expected: "https://gateway.test",
                actual: internalJwt.Issuer);
            Assert.Equal(
                expected: "https://simulationcore.test",
                actual: downstreamServices.SimulationCore);
            Assert.Equal(
                expected: "https://identity.test",
                actual: downstreamServices.Identity);
            Assert.Equal(
                expected: 9,
                actual: dashboardOptions.PanelReadTimeoutSeconds);
            Assert.Equal(
                expected: 11,
                actual: dashboardOptions.HealthProbeTimeoutSeconds);
            Assert.Equal(
                expected: 7,
                actual: dashboardOptions.MaxConcurrentCitySnapshotLoads);
            Assert.True(frontendSecurity.EnforceCookieOriginProtection);
            Assert.Contains(
                expected: "https://frontend.matrix.test",
                collection: frontendSecurity.AllowedOrigins);
            Assert.Contains(
                expected: "https://localhost:5173",
                collection: frontendSecurity.AllowedOrigins);
            Assert.Contains(
                expected: "http://localhost:5173",
                collection: frontendSecurity.AllowedOrigins);
            Assert.True(trustedForwarding.Enabled);
            Assert.Equal(
                expected: 2,
                actual: forwardedHeaders.ForwardLimit);
            Assert.Contains(
                collection: forwardedHeaders.KnownProxies,
                filter: proxy => proxy.Equals(IPAddress.Loopback));
            Assert.Contains(
                collection: forwardedHeaders.KnownProxies,
                filter: proxy => proxy.Equals(IPAddress.IPv6Loopback));
        }

        private static IConfiguration OverrideConfiguration(params (string Key, string? Value)[] overrides)
        {
            var values = BuildValidApiConfiguration()
               .AsEnumerable()
               .ToDictionary(
                    keySelector: pair => pair.Key,
                    elementSelector: pair => pair.Value,
                    comparer: StringComparer.OrdinalIgnoreCase);

            foreach ((string key, string? value) in overrides)
                values[key] = value;

            return new ConfigurationBuilder()
               .AddInMemoryCollection(values)
               .Build();
        }
    }
}
