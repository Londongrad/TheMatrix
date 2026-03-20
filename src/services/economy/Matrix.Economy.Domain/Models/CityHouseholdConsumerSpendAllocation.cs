using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Economy.Domain.Aggregates;

namespace Matrix.Economy.Domain.Models
{
    public sealed record CityHouseholdConsumerSpendAllocation(
        CityBusiness Business,
        Money GrossAmount,
        Money SalesTaxAmount,
        string SegmentKey,
        string Title,
        string Description);
}
