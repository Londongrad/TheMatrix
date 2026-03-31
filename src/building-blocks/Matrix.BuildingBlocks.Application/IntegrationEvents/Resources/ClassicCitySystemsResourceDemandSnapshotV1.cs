namespace Matrix.BuildingBlocks.Application.IntegrationEvents.Resources
{
    public sealed record ClassicCitySystemsResourceDemandSnapshotV1(
        Guid CityId,
        decimal FuelDemandPressureIndex,
        decimal SparePartsDemandPressureIndex,
        decimal FiltersDemandPressureIndex,
        decimal EmergencyWaterDemandPressureIndex,
        decimal OverallDemandPressureIndex,
        long EffectiveTickId,
        DateTimeOffset EffectiveAtUtc,
        DateTimeOffset OccurredAtUtc);
}
