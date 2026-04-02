namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.RoadAccess.GetCityRoadSegmentConditions
{
    public sealed record CityDistrictTopologyDto(
        Guid DistrictId,
        decimal AnchorX,
        decimal AnchorY);
}
