namespace Matrix.CityCore.Contracts.Cities.Requests
{
    public sealed record FailCityPopulationBootstrapRequest(
        Guid OperationId,
        string FailureCode);
}
