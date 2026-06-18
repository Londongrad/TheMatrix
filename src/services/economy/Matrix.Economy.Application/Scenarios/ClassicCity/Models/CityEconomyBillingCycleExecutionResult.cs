using Matrix.BuildingBlocks.Application.IntegrationEvents.Scenarios.ClassicCity.Population;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.HouseholdObligations.RunCityHouseholdBillingCycle;

namespace Matrix.Economy.Application.Scenarios.ClassicCity.Models
{
    public sealed record CityEconomyBillingCycleExecutionResult(
        RunCityHouseholdBillingCycleResultDto Result,
        IReadOnlyList<ClassicCityHouseholdFinancialStressBatchV1> FinancialStressBatches);
}
