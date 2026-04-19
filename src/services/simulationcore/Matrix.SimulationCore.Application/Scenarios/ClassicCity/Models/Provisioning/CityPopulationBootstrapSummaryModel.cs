namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.Models.Provisioning
{
    public sealed record CityPopulationBootstrapSummaryModel(
        Guid CityId,
        int RequestedPeopleCount,
        int GeneratedPeopleCount,
        int HouseholdCount,
        int HousedHouseholdCount,
        int HomelessHouseholdCount,
        int HousedPeopleCount,
        int HomelessPeopleCount);
}
