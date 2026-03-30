namespace Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Cities.Views
{
    public sealed record CityPopulationBootstrapSummaryView(
        Guid CityId,
        int RequestedPeopleCount,
        int GeneratedPeopleCount,
        int HouseholdCount,
        int HousedHouseholdCount,
        int HomelessHouseholdCount,
        int HousedPeopleCount,
        int HomelessPeopleCount);
}
