namespace Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Topology.Views
{
    public sealed record CityRoadGraphView(
        Guid CityId,
        DistrictView[] Districts,
        RoadSegmentView[] RoadSegments);
}
