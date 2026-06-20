namespace Matrix.ScenarioContracts.ClassicCity.IntegrationEvents.Economy
{
    public sealed record ClassicCityHouseholdCashflowSettlementBatchV1(
        Guid CityId,
        DateOnly CurrentDate,
        int SettledDays,
        int BatchNumber,
        int TotalBatches,
        IReadOnlyList<ClassicCityHouseholdCashflowSettlementItemV1> Households,
        string CorrelationId,
        DateTimeOffset OccurredAtUtc);
}
