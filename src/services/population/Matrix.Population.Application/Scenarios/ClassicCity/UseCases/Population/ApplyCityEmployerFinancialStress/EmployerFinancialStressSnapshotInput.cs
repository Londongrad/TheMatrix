namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.ApplyCityEmployerFinancialStress
{
    public sealed record EmployerFinancialStressSnapshotInput(
        string WorkplaceExternalReferenceCode,
        decimal RequestedGrossPayrollAmount,
        decimal PaidGrossPayrollAmount,
        decimal MissedGrossPayrollAmount,
        decimal PayrollFulfillmentRatio,
        int FailedPayrollCount,
        int PartialPayrollCount,
        decimal CurrentBalanceAmount,
        decimal DistressScore,
        bool HasHiringFreeze,
        bool HasLayoffPressure);
}
