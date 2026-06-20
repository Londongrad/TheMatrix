namespace Matrix.ScenarioContracts.ClassicCity.IntegrationEvents.Population
{
    public sealed record ClassicCityEmployerFinancialStressItemV1(
        Guid EmployerBusinessId,
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
