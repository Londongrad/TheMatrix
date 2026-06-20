namespace Matrix.ScenarioContracts.ClassicCity.IntegrationEvents.Population
{
    public sealed record ClassicCityEmployerFinancialStressBatchV1(
        Guid CityId,
        int BatchNumber,
        int TotalBatches,
        IReadOnlyList<ClassicCityEmployerFinancialStressItemV1> Employers,
        string CorrelationId,
        DateTimeOffset OccurredAtUtc);
}
