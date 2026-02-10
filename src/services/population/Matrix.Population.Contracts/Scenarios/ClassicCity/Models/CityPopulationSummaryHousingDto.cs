namespace Matrix.Population.Contracts.Scenarios.ClassicCity.Models
{
    public sealed record class CityPopulationSummaryHousingDto(
        int HouseholdCount,
        int HousedHouseholdCount,
        int HomelessHouseholdCount);
}
