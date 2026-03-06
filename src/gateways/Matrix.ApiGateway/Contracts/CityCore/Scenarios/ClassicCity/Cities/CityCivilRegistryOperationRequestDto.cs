namespace Matrix.ApiGateway.Contracts.CityCore.Scenarios.ClassicCity.Cities
{
    public sealed record class CityCivilRegistryOperationRequestDto(
        Guid FirstResidentId,
        Guid SecondResidentId);
}
