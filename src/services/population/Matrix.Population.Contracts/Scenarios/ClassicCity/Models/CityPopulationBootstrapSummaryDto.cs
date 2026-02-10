namespace Matrix.Population.Contracts.Scenarios.ClassicCity.Models
{
    public sealed record class CityPopulationBootstrapSummaryDto(
        Guid CityId,
        int RequestedPeopleCount,
        int GeneratedPeopleCount,
        int HouseholdCount,
        int HousedHouseholdCount,
        int HomelessHouseholdCount,
        int HousedPeopleCount,
        int HomelessPeopleCount);
}
