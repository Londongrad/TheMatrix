using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity;
using Xunit;

namespace Matrix.SimulationCore.Api.Tests.Controllers.Scenarios.ClassicCity;

public sealed class ClassicCityApiRoutesTests
{
    [Fact]
    public void Routes_AreScopedToClassicCityScenario()
    {
        Assert.Equal(
            expected: "api/scenarios/classic-city/cities",
            actual: ClassicCityApiRoutes.CitiesRoute);
        Assert.Equal(
            expected: "api/scenarios/classic-city/cities/{cityId:guid}/trips",
            actual: ClassicCityApiRoutes.TripsRoute);
        Assert.Equal(
            expected: "api/scenarios/classic-city/cities/{cityId:guid}/routes",
            actual: ClassicCityApiRoutes.RoutingRoute);
    }
}
