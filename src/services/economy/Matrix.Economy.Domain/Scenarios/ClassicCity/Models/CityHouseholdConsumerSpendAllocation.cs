using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Aggregates;

namespace Matrix.Economy.Domain.Scenarios.ClassicCity.Models
{
    public sealed record CityHouseholdConsumerSpendAllocation(
        CityBusiness Business,
        Money GrossAmount,
        Money SalesTaxAmount,
        string SegmentKey,
        string Title,
        string Description);
}
