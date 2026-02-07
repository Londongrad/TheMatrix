namespace Matrix.CityCore.Application.UseCases.Cities.CreateCity
{
    public sealed record CityCreatedDto(
        Guid CityId,
        Guid PopulationBootstrapOperationId,
        string SimulationKind);
}
