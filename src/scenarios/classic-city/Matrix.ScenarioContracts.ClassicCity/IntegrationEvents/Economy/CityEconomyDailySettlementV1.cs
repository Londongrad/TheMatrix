namespace Matrix.ScenarioContracts.ClassicCity.IntegrationEvents.Economy
{
    public sealed record CityEconomyDailySettlementV1(
        Guid CityId,
        long TickId,
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
