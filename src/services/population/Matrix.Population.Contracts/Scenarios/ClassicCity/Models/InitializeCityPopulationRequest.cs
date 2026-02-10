namespace Matrix.Population.Contracts.Scenarios.ClassicCity.Models
{
    public sealed record class InitializeCityPopulationRequest(
        Guid CityId,
        DateOnly CurrentDate,
        int PeopleCount,
        int? RandomSeed,
        CityPopulationEnvironmentDto Environment,
        IReadOnlyCollection<ResidentialBuildingSeedDto>? ResidentialBuildings);
}
