namespace Matrix.Population.Contracts.Models
{
    public sealed record class CityPopulationSummaryHousingDto(
        int HouseholdCount,
        int HousedHouseholdCount,
        int HomelessHouseholdCount);
}
