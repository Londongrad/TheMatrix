namespace Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Routing.Requests
{
    public sealed record ResolveCityRouteRequest(
        CityRoutePointRequest From,
        CityRoutePointRequest To,
        string Profile = "Pedestrian");
}
