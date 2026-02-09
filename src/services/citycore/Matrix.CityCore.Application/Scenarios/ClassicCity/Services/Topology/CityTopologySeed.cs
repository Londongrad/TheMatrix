using Matrix.CityCore.Domain.Scenarios.ClassicCity.Topology;

namespace Matrix.CityCore.Application.Scenarios.ClassicCity.Services.Topology
{
    public sealed record CityTopologySeed(
        IReadOnlyCollection<District> Districts,
        IReadOnlyCollection<ResidentialBuilding> ResidentialBuildings);
}
