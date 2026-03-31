namespace Matrix.BuildingBlocks.Application.IntegrationEvents.Resources
{
    public sealed record ClassicCityStockpileSnapshotV1(
        Guid CityId,
        decimal SupplyStressIndex,
        bool EmergencyRationingEnabled,
        ClassicCityStockpileLineSnapshotV1 Fuel,
        ClassicCityStockpileLineSnapshotV1 Food,
        ClassicCityStockpileLineSnapshotV1 Medicine,
        ClassicCityStockpileLineSnapshotV1 SpareParts,
        ClassicCityStockpileLineSnapshotV1 Filters,
        ClassicCityStockpileLineSnapshotV1 EmergencyWater,
        long EffectiveTickId,
        DateTimeOffset EffectiveAtUtc,
        DateTimeOffset OccurredAtUtc);
}
