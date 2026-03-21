namespace Matrix.ApiGateway.Contracts.SimulationCore.Scenarios.ClassicCity.Cities
{
    public sealed record class CityCivilRegistryOperationRequestDto(
        Guid FirstResidentId,
        Guid SecondResidentId);
}
