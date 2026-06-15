namespace Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.HouseholdObligations
{
    public sealed record CityHouseholdObligationDto(
        Guid ObligationId,
        Guid CityId,
        Guid HouseholdAccountId,
        Guid ProviderBusinessId,
        string CreatedAtUtc,
        string Name,
        string Kind,
        bool IsActive,
        string UnitKind,
        string UnitCode,
        string UnitDisplayName,
        string UnitSymbol,
        decimal ChargeAmount,
        decimal TaxAmount,
        string BillingCadence,
        string NextChargeDueAtUtc,
        string? LastChargedAtUtc,
        int ChargeCount);
}
