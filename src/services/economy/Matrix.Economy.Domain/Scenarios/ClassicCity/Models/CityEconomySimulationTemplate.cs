using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Models;

namespace Matrix.Economy.Domain.Scenarios.ClassicCity.Models
{
    public sealed record CityEconomySimulationTemplate(
        CityBudgetUnitProfile UnitProfile,
        Money InitialReserve,
        IReadOnlyList<CityEconomyAllocationTemplate> DefaultAllocations,
        IReadOnlyList<CityEconomyBusinessTemplate> DefaultBusinesses);
}
