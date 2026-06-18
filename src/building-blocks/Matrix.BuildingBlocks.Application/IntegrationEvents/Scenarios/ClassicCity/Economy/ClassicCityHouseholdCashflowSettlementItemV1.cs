namespace Matrix.BuildingBlocks.Application.IntegrationEvents.Scenarios.ClassicCity.Economy
{
    public sealed record ClassicCityHouseholdCashflowSettlementItemV1(
        Guid HouseholdId,
        string ExternalReferenceCode,
        decimal GrossPayrollAmount,
        decimal IncomeTaxAmount,
        decimal NetPayrollAmount,
        decimal RetailTurnoverAmount,
        decimal RetailTaxAmount,
        decimal RetailStoreSpendAmount = 0m,
        decimal ServiceSpendAmount = 0m,
        decimal MunicipalSpendAmount = 0m);
}
