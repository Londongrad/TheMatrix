namespace Matrix.BuildingBlocks.Application.IntegrationEvents.Population
{
    public sealed record ClassicCityEmployerFinancialStressItemV1(
        Guid EmployerBusinessId,
        string WorkplaceExternalReferenceCode,
        decimal RecentGrossPayrollAmount,
        decimal CurrentBalanceAmount,
        decimal DistressScore,
        bool HasHiringFreeze,
        bool HasLayoffPressure);
}
