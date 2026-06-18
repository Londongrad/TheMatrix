namespace Matrix.BuildingBlocks.Application.IntegrationEvents.Scenarios.ClassicCity.Population
{
    public sealed record ClassicCityEmployerFinancialStressBatchV1(
        Guid CityId,
        int BatchNumber,
        int TotalBatches,
        IReadOnlyList<ClassicCityEmployerFinancialStressItemV1> Employers,
        string CorrelationId,
        DateTimeOffset OccurredAtUtc);
}
