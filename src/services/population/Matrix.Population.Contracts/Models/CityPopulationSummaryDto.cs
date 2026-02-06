namespace Matrix.Population.Contracts.Models
{
    public sealed record class CityPopulationSummaryDto(
        Guid CityId,
        string CurrentDate,
        CityPopulationSummaryLifecycleDto Lifecycle,
        CityPopulationSummaryEnvironmentDto? Environment,
        CityPopulationSummarySimulationDto? Simulation,
        CityPopulationSummaryWeatherDto? Weather,
        CityPopulationSummaryHousingDto Housing,
        CityPopulationSummaryResidentsDto Residents);
}
