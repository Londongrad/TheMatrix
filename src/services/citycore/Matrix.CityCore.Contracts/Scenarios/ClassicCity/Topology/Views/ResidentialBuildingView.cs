namespace Matrix.CityCore.Contracts.Scenarios.ClassicCity.Topology.Views
{
    public sealed record ResidentialBuildingView(
        Guid ResidentialBuildingId,
        Guid CityId,
        Guid DistrictId,
        string Name,
        string Type,
        int ResidentCapacity,
        DateTimeOffset CreatedAtUtc);
}
