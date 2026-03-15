namespace Matrix.BuildingBlocks.Application.IntegrationEvents.Population
{
    public sealed record ClassicCityHouseholdFinancialStressBatchV1(
        Guid CityId,
        int BatchNumber,
        int TotalBatches,
        IReadOnlyList<ClassicCityHouseholdFinancialStressItemV1> Households,
        string CorrelationId,
        DateTimeOffset OccurredAtUtc);
}
