namespace Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.HouseholdAccounts
{
    public sealed record CityHouseholdAccountDto(
        Guid HouseholdAccountId,
        Guid CityId,
        string CreatedAtUtc,
        string Name,
        string? ExternalReferenceCode,
        string UnitKind,
        string UnitCode,
        string UnitDisplayName,
        string UnitSymbol,
        decimal Balance,
        decimal TotalOpeningBalance,
        decimal TotalPayrollIncome,
        decimal TotalConsumerSpending);
}
