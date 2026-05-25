using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Topology.GetCityAnchors;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Topology.GetCityDistricts;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Topology.GetCityResidentialBuildings;

namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Topology.GetCityMapTopology
{
    public sealed record CityMapTopologyDto(
        Guid CityId,
        IReadOnlyList<DistrictDto> Districts,
        IReadOnlyList<ResidentialBuildingDto> ResidentialBuildings,
        IReadOnlyList<CityAnchorDto> Anchors,
        IReadOnlyList<RoadNodeDto> RoadNodes,
        IReadOnlyList<RoadSegmentDto> RoadSegments);
}
