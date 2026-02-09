namespace Matrix.CityCore.Contracts.Scenarios.ClassicCity.Topology.Views
{
    public sealed record DistrictView(
        Guid DistrictId,
        Guid CityId,
        string Name,
        DateTimeOffset CreatedAtUtc);
}
