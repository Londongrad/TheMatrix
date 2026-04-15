namespace Matrix.Population.Application.Scenarios.ClassicCity.Models
{
    public sealed record CityPopulationActiveTripSnapshot(
        Guid? TravellerEntityId,
        string Subject,
        string Purpose,
        string Status,
        decimal CurrentProgressIndex,
        DateTimeOffset StartedAtSimTimeUtc,
        DateTimeOffset ExpectedArrivalAtSimTimeUtc,
        string FromName,
        Guid FromEntityId,
        string ToName,
        Guid ToEntityId);
}
