using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Topology;

namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.Topology
{
    public sealed record CityTopologySeed(
        IReadOnlyCollection<District> Districts,
        IReadOnlyCollection<ResidentialBuilding> ResidentialBuildings);
}
