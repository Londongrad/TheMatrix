namespace Matrix.Population.Contracts.Scenarios.ClassicCity.Models
{
    public sealed record class CityAnchorSeedDto(
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
