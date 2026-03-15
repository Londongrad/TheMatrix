namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.ApplyCityHouseholdFinancialStress
{
    public sealed record HouseholdFinancialStressSnapshotInput(
        string HouseholdExternalReferenceCode,
        int OverdueObligationCount,
        int OverdueRentCount,
        int OverdueUtilityCount,
        decimal TotalOverdueAmount,
        decimal DistressScore);
}
