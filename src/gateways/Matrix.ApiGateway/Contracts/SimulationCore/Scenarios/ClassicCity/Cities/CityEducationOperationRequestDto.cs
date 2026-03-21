namespace Matrix.ApiGateway.Contracts.SimulationCore.Scenarios.ClassicCity.Cities
{
    public sealed record CityEducationOperationRequestDto(
        Guid ResidentId,
        string? TargetEducationLevel,
        Guid? InstitutionId);
}
