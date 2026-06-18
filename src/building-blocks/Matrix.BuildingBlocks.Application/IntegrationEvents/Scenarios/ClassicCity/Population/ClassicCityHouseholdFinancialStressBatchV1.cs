namespace Matrix.BuildingBlocks.Application.IntegrationEvents.Scenarios.ClassicCity.Population
{
    public sealed record ClassicCityHouseholdFinancialStressBatchV1(
        Guid CityId,
        int BatchNumber,
        int TotalBatches,
        IReadOnlyList<ClassicCityHouseholdFinancialStressItemV1> Households,
        string CorrelationId,
        DateTimeOffset OccurredAtUtc);
}
