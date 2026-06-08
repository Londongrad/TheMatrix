using Matrix.Population.Api.Controllers.Scenarios.ClassicCity;
using Matrix.Population.Contracts.Scenarios.ClassicCity;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Matrix.Population.Api.Tests.Controllers.Scenarios.ClassicCity;

public sealed class ClassicCityPopulationApiRoutesTests
{
    [Fact]
    public void Routes_AreScopedToClassicCityScenario()
    {
        Assert.Equal(
            expected: "api/scenarios/classic-city/population",
            actual: ClassicCityPopulationApiRoutes.PopulationRoute);
        Assert.Equal(
            expected: "/api/scenarios/classic-city/population/init",
            actual: ClassicCityPopulationApiRoutes.InitializePath);
        Assert.Equal(
            expected: "/api/scenarios/classic-city/population/cities",
            actual: ClassicCityPopulationApiRoutes.CitiesPath);

        RouteAttribute route = Assert.Single(
            typeof(PopulationController)
               .GetCustomAttributes(
                    attributeType: typeof(RouteAttribute),
                    inherit: true)
               .Cast<RouteAttribute>());

        Assert.Equal(
            expected: ClassicCityPopulationApiRoutes.PopulationRoute,
            actual: route.Template);
    }
}
