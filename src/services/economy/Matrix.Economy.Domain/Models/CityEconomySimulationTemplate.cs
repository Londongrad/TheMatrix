namespace Matrix.Economy.Domain.Models
{
    public sealed record CityEconomySimulationTemplate(
        CityBudgetUnitProfile UnitProfile,
        IReadOnlyList<CityEconomyAllocationTemplate> DefaultAllocations);
}
