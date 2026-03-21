using Matrix.BuildingBlocks.Application.IntegrationEvents.Population;
using Matrix.Economy.Application.UseCases.HouseholdObligations.RunCityHouseholdBillingCycle;

namespace Matrix.Economy.Application.Scenarios.ClassicCity.Models
{
    public sealed record CityEconomyBillingCycleExecutionResult(
        RunCityHouseholdBillingCycleResultDto Result,
        IReadOnlyList<ClassicCityHouseholdFinancialStressBatchV1> FinancialStressBatches);
}
