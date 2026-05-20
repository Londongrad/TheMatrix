namespace Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Routing.Requests
{
    public sealed record ResolveCityRoutesBatchRequest(
        IReadOnlyList<ResolveCityRouteRequest> Routes);
}
