using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Economy.Domain.Enums;

namespace Matrix.Economy.Domain.Scenarios.ClassicCity.Models
{
    public sealed record CityEconomyAllocationTemplate(
        CityBudgetCategory Category,
        Money TargetAmount);
}
