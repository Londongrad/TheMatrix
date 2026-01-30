namespace Matrix.Population.Application.UseCases.Population.InitializeCityPopulation
{
    public sealed record ResidentialBuildingSeedItem(
        Guid ResidentialBuildingId,
        Guid DistrictId,
        int ResidentCapacity);
}
