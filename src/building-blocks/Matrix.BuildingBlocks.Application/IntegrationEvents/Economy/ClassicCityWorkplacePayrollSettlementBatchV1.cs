namespace Matrix.BuildingBlocks.Application.IntegrationEvents.Economy
{
    public sealed record ClassicCityWorkplacePayrollSettlementBatchV1(
        Guid CityId,
        DateOnly CurrentDate,
        int SettledDays,
        int BatchNumber,
        int TotalBatches,
        IReadOnlyList<ClassicCityWorkplacePayrollSettlementItemV1> Payrolls,
        string CorrelationId,
        DateTimeOffset OccurredAtUtc);
}
