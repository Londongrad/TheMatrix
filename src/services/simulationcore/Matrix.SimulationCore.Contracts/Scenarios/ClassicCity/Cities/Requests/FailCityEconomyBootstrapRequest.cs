namespace Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Cities.Requests
{
    public sealed record FailCityEconomyBootstrapRequest(
        Guid OperationId,
        string FailureCode);
}
