namespace Matrix.BuildingBlocks.Application.IntegrationEvents.Population
{
    public sealed record ClassicCityHouseholdFinancialStressItemV1(
        Guid HouseholdAccountId,
        string HouseholdExternalReferenceCode,
        int OverdueObligationCount,
        int OverdueRentCount,
        int OverdueUtilityCount,
        decimal TotalOverdueAmount,
        decimal DistressScore);
}
