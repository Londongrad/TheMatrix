namespace Matrix.Population.Contracts.Scenarios.ClassicCity.Models
{
    public sealed record class InitializeCityPopulationRequest(
        Guid CityId,
        DateOnly CurrentDate,
        DateTimeOffset CreatedAtUtc,
        int PeopleCount,
        int RandomSeed,
        CityPopulationEnvironmentDto Environment,
        CityPopulationBootstrapTuningDto Tuning,
        IReadOnlyCollection<ResidentialBuildingSeedDto>? ResidentialBuildings);
}
