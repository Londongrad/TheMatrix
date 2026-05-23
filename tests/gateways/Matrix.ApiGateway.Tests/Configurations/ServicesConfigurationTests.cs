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
using Matrix.ApiGateway.Services.SimulationCore.Dashboard;
using Matrix.ApiGateway.Services.SimulationCore.Scenarios.ClassicCity.Cities;
using Matrix.BuildingBlocks.Api.Forwarding;
using Matrix.BuildingBlocks.Application.Authorization.Jwt;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using Xunit;
using static Matrix.ApiGateway.Tests.TestSupport.StartupTestSupport;

namespace Matrix.ApiGateway.Tests.Configurations;

public sealed class ServicesConfigurationTests
{
    [Fact]
    public void ConfigureApplicationServices_WhenRedisConnectionStringIsMissing_ThrowsWhenMultiplexerIsResolved()
    {
        IConfiguration configuration = OverrideConfiguration(("Redis:ConnectionString", ""));
        WebApplicationBuilder builder = CreateBuilder(configuration);

        builder.ConfigureApplicationServices();

        using ServiceProvider provider = builder.Services.BuildServiceProvider();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => provider.GetRequiredService<IConnectionMultiplexer>());

        Assert.Contains("Redis:ConnectionString is required for gateway session orchestration.", exception.Message);
    }

    [Fact]
    public void ConfigureApplicationServices_WhenConfigurationIsValid_RegistersGatewayServicesAuthAndTypedClients()
    {
        WebApplicationBuilder builder = CreateBuilder(BuildValidApiConfiguration());

        builder.ConfigureApplicationServices();

        using ServiceProvider provider = builder.Services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        AuthenticationOptions authentication = provider.GetRequiredService<IOptions<AuthenticationOptions>>().Value;
        DownstreamServicesOptions downstreamServices =
            provider.GetRequiredService<IOptions<DownstreamServicesOptions>>().Value;
        CityOperationsDashboardOptions dashboardOptions =
            provider.GetRequiredService<IOptions<CityOperationsDashboardOptions>>().Value;
        FrontendSecurityOptions frontendSecurity =
            provider.GetRequiredService<IOptions<FrontendSecurityOptions>>().Value;
        TrustedForwardedHeadersOptions trustedForwarding =
            provider.GetRequiredService<IOptions<TrustedForwardedHeadersOptions>>().Value;
        ForwardedHeadersOptions forwardedHeaders =
            provider.GetRequiredService<IOptions<ForwardedHeadersOptions>>().Value;
        InternalUserContextJwtOptions internalJwt =
            provider.GetRequiredService<IOptions<InternalUserContextJwtOptions>>().Value;

        Assert.Equal(JwtBearerDefaults.AuthenticationScheme, authentication.DefaultAuthenticateScheme);
        Assert.Equal(JwtBearerDefaults.AuthenticationScheme, authentication.DefaultChallengeScheme);
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IInternalJwtIssuer>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IInternalJwtRequestContextAccessor>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ICityOperationsDashboardAlertBuilder>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ICityOperationsDashboardService>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ICityProvisioningService>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ICitiesApiClient>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IPopulationApiClient>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IEconomyApiClient>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IIdentityAuthClient>());
        Assert.Same(TimeProvider.System, provider.GetRequiredService<TimeProvider>());

        Assert.Equal("https://gateway.test", internalJwt.Issuer);
        Assert.Equal("https://simulationcore.test", downstreamServices.SimulationCore);
        Assert.Equal("https://identity.test", downstreamServices.Identity);
        Assert.Equal(9, dashboardOptions.PanelReadTimeoutSeconds);
        Assert.Equal(11, dashboardOptions.HealthProbeTimeoutSeconds);
        Assert.Equal(7, dashboardOptions.MaxConcurrentCitySnapshotLoads);
        Assert.True(frontendSecurity.EnforceCookieOriginProtection);
        Assert.Contains("https://frontend.matrix.test", frontendSecurity.AllowedOrigins);
        Assert.Contains("https://localhost:5173", frontendSecurity.AllowedOrigins);
        Assert.Contains("http://localhost:5173", frontendSecurity.AllowedOrigins);
        Assert.True(trustedForwarding.Enabled);
        Assert.Equal(2, forwardedHeaders.ForwardLimit);
        Assert.Contains(forwardedHeaders.KnownProxies, proxy => proxy.Equals(IPAddress.Loopback));
        Assert.Contains(forwardedHeaders.KnownProxies, proxy => proxy.Equals(IPAddress.IPv6Loopback));
    }

    private static IConfiguration OverrideConfiguration(params (string Key, string? Value)[] overrides)
    {
        Dictionary<string, string?> values = BuildValidApiConfiguration()
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
