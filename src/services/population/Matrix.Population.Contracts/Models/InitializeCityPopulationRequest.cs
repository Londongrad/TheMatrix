namespace Matrix.Population.Contracts.Models
{
    public sealed record class InitializeCityPopulationRequest(
        Guid CityId,
        DateOnly CurrentDate,
        int PeopleCount,
        int? RandomSeed,
        IReadOnlyCollection<ResidentialBuildingSeedDto>? ResidentialBuildings);
}
