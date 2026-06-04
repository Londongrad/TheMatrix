using System.Reflection;
using Matrix.ApiGateway.Configurations.Options;
using Matrix.ApiGateway.DownstreamClients.Economy.Scenarios.ClassicCity;
using Matrix.ApiGateway.DownstreamClients.Population.People;
using Matrix.ApiGateway.DownstreamClients.Resources.Scenarios.ClassicCity.Stockpiles;
using Matrix.ApiGateway.DownstreamClients.SimulationCore.Scenarios.ClassicCity.Trips;
using Matrix.ApiGateway.DownstreamClients.SimulationSystems.Scenarios.ClassicCity.EnvironmentalConditions;
using Matrix.ApiGateway.Services.SimulationCore.Scenarios.ClassicCity.Dashboard;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Xunit;

namespace Matrix.ApiGateway.Tests.Services.SimulationCore.Scenarios.ClassicCity.Dashboard
{
    public sealed class CityOperationsDashboardCompositionTests
    {
        [Fact]
        public void CityOperationsDashboardService_DoesNotDependOnExtractedDownstreamClients()
        {
            Type[] forbiddenTypes =
            [
                typeof(IClassicCityEconomyApiClient),
                typeof(IPopulationApiClient),
                typeof(IStockpilesApiClient),
                typeof(ITripsApiClient),
                typeof(IEnvironmentalConditionsApiClient),
                typeof(HealthCheckService),
                typeof(IHttpClientFactory),
                typeof(IOptions<DownstreamServicesOptions>)
            ];
            Type[] constructorParameterTypes = GetConstructorParameterTypes();

            foreach (Type forbiddenType in forbiddenTypes)
                Assert.DoesNotContain(
                    expected: forbiddenType,
                    collection: constructorParameterTypes);
        }

        [Fact]
        public void CityOperationsDashboardService_DependsOnExtractedDashboardServices()
        {
            Type[] constructorParameterTypes = GetConstructorParameterTypes();

            Assert.Contains(
                expected: typeof(ICityOperationsDashboardHealthProbe),
                collection: constructorParameterTypes);
            Assert.Contains(
                expected: typeof(ICityOperationsDashboardSnapshotLoader),
                collection: constructorParameterTypes);
            Assert.Contains(
                expected: typeof(ICityOperationsDashboardAlertBuilder),
                collection: constructorParameterTypes);
            Assert.Contains(
                expected: typeof(ICityOperationsDashboardRecentEventsBuilder),
                collection: constructorParameterTypes);
        }

        private static Type[] GetConstructorParameterTypes()
        {
            ConstructorInfo constructor = Assert.Single(
                typeof(CityOperationsDashboardService).GetConstructors(
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic));

            return constructor
               .GetParameters()
               .Select(parameter => parameter.ParameterType)
               .ToArray();
        }
    }
}
