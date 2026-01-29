using Matrix.CityCore.Domain.Topology;

namespace Matrix.CityCore.Application.Services.Topology
{
    public sealed record CityTopologySeed(
        IReadOnlyCollection<District> Districts,
        IReadOnlyCollection<ResidentialBuilding> ResidentialBuildings);
}