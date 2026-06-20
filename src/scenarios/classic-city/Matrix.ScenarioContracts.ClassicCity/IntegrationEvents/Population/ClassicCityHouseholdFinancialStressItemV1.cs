namespace Matrix.ScenarioContracts.ClassicCity.IntegrationEvents.Population
{
    public sealed record ClassicCityHouseholdFinancialStressItemV1(
        Guid HouseholdAccountId,
        string HouseholdExternalReferenceCode,
        int OverdueObligationCount,
        int OverdueRentCount,
        int OverdueUtilityCount,
        int ArrearsObligationCount,
        int ServiceCutoffCount,
        int EvictionNoticeCount,
        int EvictionEligibleCount,
        int OldestOverdueAgeDays,
        decimal TotalOverdueAmount,
        decimal DistressScore);
}
