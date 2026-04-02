namespace Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Topology.Views
{
    public sealed record CityMapTopologyView(
        Guid CityId,
        DistrictView[] Districts,
        ResidentialBuildingView[] ResidentialBuildings,
        CityAnchorView[] Anchors,
        RoadNodeView[] RoadNodes,
        RoadSegmentView[] RoadSegments);
}
