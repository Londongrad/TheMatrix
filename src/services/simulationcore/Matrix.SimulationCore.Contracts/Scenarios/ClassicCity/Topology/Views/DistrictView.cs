namespace Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Topology.Views
{
    public sealed record DistrictView(
        Guid DistrictId,
        Guid CityId,
        string Name,
        decimal AnchorX,
        decimal AnchorY,
        DateTimeOffset CreatedAtUtc);
}
