namespace Matrix.Economy.Application.UseCases.Simulation.AdvanceCityEconomy
{
    public sealed record AdvanceCityEconomySimulationResult(
        AdvanceCityEconomySimulationStatus Status,
        int ProcessedDays,
        int ChargedObligations,
        int RemittedBusinesses,
        int MunicipalProviderPayments,
        decimal TotalChargedAmount,
        decimal TotalTaxRemittedAmount,
        decimal TotalMunicipalDisbursedAmount);
}
