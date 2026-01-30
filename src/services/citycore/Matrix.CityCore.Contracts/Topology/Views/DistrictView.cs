namespace Matrix.CityCore.Contracts.Topology.Views
{
    public sealed record DistrictView(
        Guid DistrictId,
        Guid CityId,
        string Name,
        DateTimeOffset CreatedAtUtc);
}
