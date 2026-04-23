using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Topology.GetCityDistricts;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Topology.GetCityMapTopology;

namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Topology.GetCityRoadGraph
{
    public sealed record CityRoadGraphDto(
        Guid CityId,
        IReadOnlyList<DistrictDto> Districts,
        IReadOnlyList<RoadSegmentDto> RoadSegments);
}
