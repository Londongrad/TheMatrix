using Matrix.BuildingBlocks.Domain.ValueObjects;

namespace Matrix.Economy.Domain.Models
{
    public sealed record CityEconomySimulationTemplate(
        CityBudgetUnitProfile UnitProfile,
        Money InitialReserve,
        IReadOnlyList<CityEconomyAllocationTemplate> DefaultAllocations,
        IReadOnlyList<CityEconomyBusinessTemplate> DefaultBusinesses);
}
