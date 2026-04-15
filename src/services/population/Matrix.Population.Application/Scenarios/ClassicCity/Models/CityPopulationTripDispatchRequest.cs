namespace Matrix.Population.Application.Scenarios.ClassicCity.Models
{
    public sealed record CityPopulationTripDispatchRequest(
        Guid CityId,
        string FromKind,
        Guid FromId,
        string ToKind,
        Guid ToId,
        string Purpose,
        string Profile,
        decimal MovementCapabilityIndex,
        Guid? TravellerEntityId,
        string? Subject);
}
