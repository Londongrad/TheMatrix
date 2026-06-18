namespace Matrix.BuildingBlocks.Application.IntegrationEvents.Scenarios.ClassicCity.Economy
{
    public sealed record ClassicCityWorkplaceBusinessSyncBatchV1(
        Guid CityId,
        int BatchNumber,
        int TotalBatches,
        IReadOnlyList<ClassicCityWorkplaceBusinessSyncItemV1> Workplaces,
        string CorrelationId,
        DateTimeOffset OccurredAtUtc);
}
