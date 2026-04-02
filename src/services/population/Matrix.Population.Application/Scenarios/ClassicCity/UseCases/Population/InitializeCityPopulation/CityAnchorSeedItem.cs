namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.InitializeCityPopulation
{
    public sealed record CityAnchorSeedItem(
        Guid CityAnchorId,
        Guid DistrictId,
        Guid AccessRoadNodeId,
        string Name,
        string Type,
        int Capacity,
        decimal PositionX,
        decimal PositionY,
        DateTimeOffset CreatedAtUtc);
}
