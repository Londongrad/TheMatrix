namespace Matrix.ApiGateway.Contracts.CityCore.Scenarios.ClassicCity.Cities
{
    public sealed record CityEducationOperationRequestDto(
        Guid ResidentId,
        string? TargetEducationLevel);
}
