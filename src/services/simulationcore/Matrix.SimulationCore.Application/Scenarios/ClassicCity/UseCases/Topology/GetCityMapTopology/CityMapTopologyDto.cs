namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Topology.GetCityMapTopology
{
    public sealed record CityMapTopologyDto(
        Guid CityId,
        IReadOnlyList<GetCityDistricts.DistrictDto> Districts,
        IReadOnlyList<GetCityResidentialBuildings.ResidentialBuildingDto> ResidentialBuildings,
        IReadOnlyList<GetCityAnchors.CityAnchorDto> Anchors,
        IReadOnlyList<RoadNodeDto> RoadNodes,
        IReadOnlyList<RoadSegmentDto> RoadSegments);
}
