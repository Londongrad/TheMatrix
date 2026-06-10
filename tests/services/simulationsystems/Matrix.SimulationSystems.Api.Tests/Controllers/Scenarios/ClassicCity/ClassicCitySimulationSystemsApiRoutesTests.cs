using System.Reflection;
using Matrix.SimulationSystems.Api.Controllers.Scenarios.ClassicCity;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Matrix.SimulationSystems.Api.Tests.Controllers.Scenarios.ClassicCity
{
    public sealed class ClassicCitySimulationSystemsApiRoutesTests
    {
        public static TheoryData<Type> ControllerTypes =>
            new()
            {
                typeof(DrainageController),
                typeof(EnvironmentalConditionsController),
                typeof(HeatingController),
                typeof(PowerDistributionController),
                typeof(RoadAccessController),
                typeof(SanitationController),
                typeof(SnowRemovalController),
                typeof(UtilityIncidentsController),
                typeof(WaterDistributionController)
            };

        [Fact]
        public void CitiesRoute_IsScenarioScoped()
        {
            Assert.Equal(
                expected: "api/scenarios/classic-city/cities",
                actual: ClassicCitySimulationSystemsApiRoutes.CitiesRoute);
        }

        [Theory]
        [MemberData(nameof(ControllerTypes))]
        public void Controller_UsesScenarioRouteContract(Type controllerType)
        {
            RouteAttribute? route = controllerType.GetCustomAttribute<RouteAttribute>();

            Assert.NotNull(route);
            Assert.Equal(
                expected: ClassicCitySimulationSystemsApiRoutes.CitiesRoute,
                actual: route.Template);
        }
    }
}
