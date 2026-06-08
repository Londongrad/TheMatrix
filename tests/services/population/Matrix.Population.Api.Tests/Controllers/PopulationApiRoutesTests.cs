using Matrix.Population.Api.Controllers;
using Matrix.Population.Contracts;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Matrix.Population.Api.Tests.Controllers;

public sealed class PopulationApiRoutesTests
{
    [Fact]
    public void SharedRoutes_DescribePeopleWithoutScenarioVocabulary()
    {
        Assert.Equal(
            expected: "api/population/people",
            actual: PopulationApiRoutes.PeopleRoute);
        Assert.Equal(
            expected: "api/population/people/{personId:guid}",
            actual: PopulationApiRoutes.PersonRoute);
        Assert.Equal(
            expected: PopulationApiRoutes.PeopleRoute,
            actual: GetRouteTemplate<PeopleController>());
        Assert.Equal(
            expected: PopulationApiRoutes.PersonRoute,
            actual: GetRouteTemplate<PersonController>());
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
