using Matrix.BuildingBlocks.Application.IntegrationEvents.Population;
using Matrix.Economy.Application.UseCases.HouseholdObligations.RunCityHouseholdBillingCycle;

namespace Matrix.Economy.Application.UseCases.Simulation.Common
{
    public sealed record CityEconomyBillingCycleExecutionResult(
        RunCityHouseholdBillingCycleResultDto Result,
        IReadOnlyList<ClassicCityHouseholdFinancialStressBatchV1> FinancialStressBatches);
}
