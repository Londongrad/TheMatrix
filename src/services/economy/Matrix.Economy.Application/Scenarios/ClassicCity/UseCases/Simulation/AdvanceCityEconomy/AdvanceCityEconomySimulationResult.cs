namespace Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.Simulation.AdvanceCityEconomy
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
