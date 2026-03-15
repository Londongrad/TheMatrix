namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.ApplyCityHouseholdFinancialStress
{
    public sealed record ApplyCityHouseholdFinancialStressResult(
        ApplyCityHouseholdFinancialStressStatus Status,
        int AppliedHouseholdCount);
}
