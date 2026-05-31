namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.CreateCity
{
    public sealed record CityCreatedDto(
        Guid CityId,
        Guid PopulationBootstrapOperationId,
        Guid EconomyBootstrapOperationId);
}
