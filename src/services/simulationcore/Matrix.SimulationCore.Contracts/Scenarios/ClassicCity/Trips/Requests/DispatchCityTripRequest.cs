using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Routing.Requests;

namespace Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Trips.Requests
{
    public sealed record DispatchCityTripRequest(
        CityRoutePointRequest From,
        CityRoutePointRequest To,
        string Purpose = "WorkCommute",
        string Profile = "Pedestrian",
        decimal MovementCapabilityIndex = 1.0m,
        Guid? TravellerEntityId = null,
        string? Subject = null);
}
