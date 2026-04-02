namespace Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Routing.Requests
{
    public sealed record CityRoutePointRequest(
        string Kind,
        Guid Id);
}
