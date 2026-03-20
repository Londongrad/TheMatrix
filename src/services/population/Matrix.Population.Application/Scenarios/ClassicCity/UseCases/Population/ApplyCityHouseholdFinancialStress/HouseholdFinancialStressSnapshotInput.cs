namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.ApplyCityHouseholdFinancialStress
{
    public sealed record HouseholdFinancialStressSnapshotInput(
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
