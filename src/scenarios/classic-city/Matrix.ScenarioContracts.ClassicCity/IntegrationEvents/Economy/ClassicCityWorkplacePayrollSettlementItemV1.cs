namespace Matrix.ScenarioContracts.ClassicCity.IntegrationEvents.Economy
{
    public sealed record ClassicCityWorkplacePayrollSettlementItemV1(
        Guid HouseholdId,
        string HouseholdExternalReferenceCode,
        Guid WorkplaceId,
        string WorkplaceExternalReferenceCode,
        string JobTitle,
        decimal GrossPayrollAmount,
        decimal IncomeTaxAmount,
        decimal NetPayrollAmount);
}
