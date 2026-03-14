namespace Matrix.BuildingBlocks.Application.IntegrationEvents.Economy
{
    public sealed record ClassicCityHouseholdCashflowSettlementItemV1(
        Guid HouseholdId,
        string ExternalReferenceCode,
        decimal GrossPayrollAmount,
        decimal IncomeTaxAmount,
        decimal NetPayrollAmount,
        decimal RetailTurnoverAmount,
        decimal RetailTaxAmount);
}
