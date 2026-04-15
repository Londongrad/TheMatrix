namespace Matrix.Population.Application.Scenarios.ClassicCity.Models
{
    public sealed record CityPopulationActiveTripSnapshot(
        Guid? TravellerEntityId,
        string Purpose,
        string Status,
        Guid FromEntityId,
        Guid ToEntityId);
}
