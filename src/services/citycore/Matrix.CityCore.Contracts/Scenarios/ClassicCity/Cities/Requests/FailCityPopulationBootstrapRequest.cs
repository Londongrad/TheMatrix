namespace Matrix.CityCore.Contracts.Scenarios.ClassicCity.Cities.Requests
{
    public sealed record FailCityPopulationBootstrapRequest(
        Guid OperationId,
        string FailureCode);
}
