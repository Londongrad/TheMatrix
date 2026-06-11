using System.Reflection;
using Matrix.BuildingBlocks.Application.Authorization.Jwt;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.SimulationSystems.Infrastructure.Options;
using Matrix.SimulationSystems.Infrastructure.Scenarios.ClassicCity;
using Matrix.SimulationSystems.Infrastructure.Scenarios.ClassicCity.Integrations.Economy;
using Matrix.SimulationSystems.Infrastructure.SimulationCore;
using Matrix.SimulationSystems.Infrastructure.Tests.TestSupport;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Xunit;

namespace Matrix.SimulationSystems.Infrastructure.Tests
{
    public sealed class DependencyInjectionTests
    {
        [Fact]
        public void AddInfrastructure_WhenConnectionStringIsMissing_ThrowsInvalidOperationException()
        {
            var services = new ServiceCollection();
            IConfiguration configuration = new ConfigurationBuilder().Build();

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(()
                => services.AddInfrastructure(
                    configuration: configuration,
                    environment: new FakeHostEnvironment()));

            Assert.Contains(
                expectedSubstring: "Connection string 'SimulationSystemsDb' is not configured",
                actualString: exception.Message);
        }

        [Fact]
        public void AddInfrastructure_WhenScenarioIsNotComposed_DoesNotRegisterClassicCityServices()
        {
            var services = new ServiceCollection();

            services.AddInfrastructure(
                configuration: StartupTestSupport.BuildValidInfrastructureConfiguration(),
                environment: new FakeHostEnvironment());

            using ServiceProvider serviceProvider = services.BuildServiceProvider();

            Assert.Null(serviceProvider.GetService<ICityEnvironmentalConditionRepository>());
            Assert.Null(serviceProvider.GetService<ICityBudgetAuthorizationClient>());
        }

        [Fact]
        public void AddInfrastructureAndClassicCityScenario_WhenConfigurationIsValid_RegistersKeyServices()
        {
            var services = new ServiceCollection();
            IConfiguration configuration = StartupTestSupport.BuildValidInfrastructureConfiguration();
            services.AddOptions<InternalServiceJwtOptions>()
               .Bind(
                    StartupTestSupport.BuildInternalServiceJwtConfiguration()
                       .GetSection(InternalServiceJwtOptions.SectionName));
            services.AddInfrastructure(
                configuration: configuration,
                environment: new FakeHostEnvironment
                {
                    EnvironmentName = Environments.Development
                },
                configureConsumers: consumers => consumers.AddClassicCityScenarioConsumers());
            services.AddClassicCityScenarioInfrastructure();

            using ServiceProvider serviceProvider = services.BuildServiceProvider();

            Assert.Same(
                expected: TimeProvider.System,
                actual: serviceProvider.GetRequiredService<TimeProvider>());
            Assert.NotNull(serviceProvider.GetRequiredService<ICityEnvironmentalConditionRepository>());

            DownstreamServicesOptions downstreamOptions = serviceProvider
               .GetRequiredService<IOptions<DownstreamServicesOptions>>()
               .Value;
            Assert.Equal(
                expected: "https://economy.test",
                actual: downstreamOptions.Economy);
            Assert.Equal(
                expected: "https://simulationcore.test",
                actual: downstreamOptions.SimulationCore);

            CityBudgetAuthorizationClient budgetClient =
                Assert.IsType<CityBudgetAuthorizationClient>(
                    serviceProvider.GetRequiredService<ICityBudgetAuthorizationClient>());
            CityMapTopologyClient topologyClient =
                Assert.IsType<CityMapTopologyClient>(serviceProvider.GetRequiredService<ICityMapTopologyClient>());
            CityOperationalTripDispatcher dispatcher =
                Assert.IsType<CityOperationalTripDispatcher>(
                    serviceProvider.GetRequiredService<ICityOperationalTripDispatcher>());

            Assert.Equal(
                expected: "https://economy.test/",
                actual: ExtractBaseAddress(budgetClient)
                   .ToString());
            Assert.Equal(
                expected: "https://simulationcore.test/",
                actual: ExtractBaseAddress(topologyClient)
                   .ToString());
            Assert.Equal(
                expected: "https://simulationcore.test/",
                actual: ExtractBaseAddress(dispatcher)
                   .ToString());
        }

        private static Uri ExtractBaseAddress(object client)
        {
            FieldInfo field = client.GetType()
                                 .GetField(
                                      name: "_client",
                                      bindingAttr: BindingFlags.Instance | BindingFlags.NonPublic) ??
                              throw new InvalidOperationException(
                                  $"Could not find _client field on {client.GetType().FullName}.");

            HttpClient httpClient = Assert.IsType<HttpClient>(field.GetValue(client));
            return Assert.IsType<Uri>(httpClient.BaseAddress);
        }
    }
}
