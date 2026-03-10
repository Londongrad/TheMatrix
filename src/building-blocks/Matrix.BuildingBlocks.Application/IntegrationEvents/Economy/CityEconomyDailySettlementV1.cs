namespace Matrix.BuildingBlocks.Application.IntegrationEvents.Economy
{
    public sealed record CityEconomyDailySettlementV1(
        Guid CityId,
        Guid TickId,
        DateOnly CurrentDate,
        int SettledDays,
        int HouseholdCount,
        int ResidentCount,
        decimal GrossPayrollAmount,
        decimal IncomeTaxAmount,
        decimal NetPayrollAmount,
        decimal RetailTurnoverAmount,
        decimal RetailTaxAmount,
        decimal HousingSpendAmount,
        string CorrelationId,
        DateTimeOffset OccurredAtUtc);
}
