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
        Assert.Equal(
            expected: ClassicCityPopulationApiRoutes.PopulationRoute,
            actual: GetRouteTemplate<ClassicCityPopulationBootstrapController>());
        Assert.Equal(
            expected: "api/scenarios/classic-city/population/cities/{cityId:guid}",
            actual: ClassicCityPopulationApiRoutes.CityRoute);
        Assert.Equal(
            expected: ClassicCityPopulationApiRoutes.CityRoute,
            actual: GetRouteTemplate<ClassicCityPopulationStateController>());
        Assert.Equal(
            expected: ClassicCityPopulationApiRoutes.ResidentsRoute,
            actual: GetRouteTemplate<ClassicCityResidentsController>());
        Assert.Equal(
            expected: ClassicCityPopulationApiRoutes.EmploymentRoute,
            actual: GetRouteTemplate<ClassicCityEmploymentController>());
        Assert.Equal(
            expected: ClassicCityPopulationApiRoutes.CivilRegistryRoute,
            actual: GetRouteTemplate<ClassicCityCivilRegistryController>());
    }

    private static string? GetRouteTemplate<TController>()
    {
        return Assert.Single(
                typeof(TController)
                   .GetCustomAttributes(
                        attributeType: typeof(RouteAttribute),
                        inherit: true)
                   .Cast<RouteAttribute>())
           .Template;
    }
}
