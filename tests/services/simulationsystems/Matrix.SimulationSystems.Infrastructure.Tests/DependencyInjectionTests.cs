using System.Reflection;
using Matrix.BuildingBlocks.Application.Authorization.Jwt;
using Matrix.SimulationSystems.Application.Abstractions;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.SimulationSystems.Infrastructure.Economy;
using Matrix.SimulationSystems.Infrastructure.Options;
using Matrix.SimulationSystems.Infrastructure.SimulationCore;
using Matrix.SimulationSystems.Infrastructure.Tests.TestSupport;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Xunit;

namespace Matrix.SimulationSystems.Infrastructure.Tests;

public sealed class DependencyInjectionTests
{
    [Fact]
    public void AddInfrastructure_WhenConnectionStringIsMissing_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        IConfiguration configuration = new ConfigurationBuilder().Build();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => services.AddInfrastructure(configuration, new FakeHostEnvironment()));

        Assert.Contains("Connection string 'SimulationSystemsDb' is not configured", exception.Message);
    }

    [Fact]
    public void AddInfrastructure_WhenConfigurationIsValid_RegistersKeyServices()
    {
        var services = new ServiceCollection();
        IConfiguration configuration = StartupTestSupport.BuildValidInfrastructureConfiguration();
        services.AddOptions<InternalServiceJwtOptions>()
            .Bind(StartupTestSupport.BuildInternalServiceJwtConfiguration().GetSection(InternalServiceJwtOptions.SectionName));
        services.AddInfrastructure(configuration, new FakeHostEnvironment { EnvironmentName = Environments.Development });

        using ServiceProvider serviceProvider = services.BuildServiceProvider();

        Assert.Same(TimeProvider.System, serviceProvider.GetRequiredService<TimeProvider>());
        Assert.NotNull(serviceProvider.GetRequiredService<ICityEnvironmentalConditionRepository>());

        DownstreamServicesOptions downstreamOptions = serviceProvider.GetRequiredService<IOptions<DownstreamServicesOptions>>().Value;
        Assert.Equal("https://economy.test", downstreamOptions.Economy);
        Assert.Equal("https://simulationcore.test", downstreamOptions.SimulationCore);

        var budgetClient = Assert.IsType<CityBudgetAuthorizationClient>(serviceProvider.GetRequiredService<ICityBudgetAuthorizationClient>());
        var topologyClient = Assert.IsType<CityMapTopologyClient>(serviceProvider.GetRequiredService<ICityMapTopologyClient>());
        var dispatcher = Assert.IsType<CityOperationalTripDispatcher>(serviceProvider.GetRequiredService<ICityOperationalTripDispatcher>());

        Assert.Equal("https://economy.test/", ExtractBaseAddress(budgetClient).ToString());
        Assert.Equal("https://simulationcore.test/", ExtractBaseAddress(topologyClient).ToString());
        Assert.Equal("https://simulationcore.test/", ExtractBaseAddress(dispatcher).ToString());
    }

    private static Uri ExtractBaseAddress(object client)
    {
        FieldInfo field = client.GetType().GetField("_client", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Could not find _client field on {client.GetType().FullName}.");

        var httpClient = Assert.IsType<HttpClient>(field.GetValue(client));
        return Assert.IsType<Uri>(httpClient.BaseAddress);
    }
}
