namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.RoadAccess.GetCityRoadSegmentConditions
{
    public sealed record CityRoadGraphTopologyDto(
        Guid CityId,
        IReadOnlyList<CityDistrictTopologyDto> Districts,
        IReadOnlyList<CityRoadSegmentTopologyDto> RoadSegments);
}
