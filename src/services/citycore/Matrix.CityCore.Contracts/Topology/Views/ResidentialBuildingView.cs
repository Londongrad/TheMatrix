namespace Matrix.CityCore.Contracts.Topology.Views
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