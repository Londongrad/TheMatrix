namespace Matrix.ApiGateway.Contracts.CityCore.Scenarios.ClassicCity.Cities
{
    public sealed record CityEmploymentOperationRequestDto(
        Guid ResidentId,
        string? JobTitle);
}
