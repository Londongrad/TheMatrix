namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.ApplyCityEmployerFinancialStress
{
    public sealed record EmployerFinancialStressSnapshotInput(
        string WorkplaceExternalReferenceCode,
        decimal RecentGrossPayrollAmount,
        decimal CurrentBalanceAmount,
        decimal DistressScore,
        bool HasHiringFreeze,
        bool HasLayoffPressure);
}
