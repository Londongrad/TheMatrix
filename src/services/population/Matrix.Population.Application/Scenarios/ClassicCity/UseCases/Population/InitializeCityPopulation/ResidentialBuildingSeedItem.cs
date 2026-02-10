namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.InitializeCityPopulation
{
    public sealed record ResidentialBuildingSeedItem(
        Guid ResidentialBuildingId,
        Guid DistrictId,
        int ResidentCapacity);
}
