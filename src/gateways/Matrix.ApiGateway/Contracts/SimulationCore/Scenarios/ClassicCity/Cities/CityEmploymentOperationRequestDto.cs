namespace Matrix.ApiGateway.Contracts.SimulationCore.Scenarios.ClassicCity.Cities
{
    public sealed record CityEmploymentOperationRequestDto(
        Guid ResidentId,
        string? JobTitle,
        Guid? WorkplaceId);
}
