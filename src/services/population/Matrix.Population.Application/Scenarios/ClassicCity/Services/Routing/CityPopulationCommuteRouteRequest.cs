using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;

namespace Matrix.Population.Application.Scenarios.ClassicCity.Services.Routing
{
    public readonly record struct CityPopulationCommuteRouteRequest(
        ResidentialBuildingId ResidentialBuildingId,
        CityAnchorId DestinationAnchorId,
        string Profile);
}
