namespace Matrix.BuildingBlocks.Application.IntegrationEvents.Scenarios.ClassicCity.Economy
{
    public sealed record ClassicCityHouseholdAccountSyncBatchV1(
        Guid CityId,
        int BatchNumber,
        int TotalBatches,
        IReadOnlyList<ClassicCityHouseholdAccountSyncItemV1> Households,
        string CorrelationId,
        DateTimeOffset OccurredAtUtc);
}
