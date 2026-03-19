namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.ApplyCityEmployerFinancialStress
{
    public sealed record ApplyCityEmployerFinancialStressResult(
        ApplyCityEmployerFinancialStressStatus Status,
        int AppliedEmployerCount);
}
