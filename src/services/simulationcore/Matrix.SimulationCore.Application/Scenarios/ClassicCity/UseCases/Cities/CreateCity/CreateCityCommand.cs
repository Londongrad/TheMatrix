using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.SimulationCore.Application.Authorization.Permissions;
using MediatR;

namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.CreateCity
{
    public sealed record CreateCityCommand(
        string Name,
        string ClimateZone,
        string Hemisphere,
        int UtcOffsetMinutes,
        string? GenerationSeed,
        string? SizeTier,
        string? UrbanDensity,
        string? DevelopmentLevel,
        string? EconomyProfile,
        string? PopulationOccupancyProfile,
        string? InitialWeatherMode,
        string? InitialWeatherType,
        string? InitialWeatherSeverity,
        decimal? InitialWeatherTemperatureC,
        DateTimeOffset StartSimTimeUtc,
        decimal SpeedMultiplier = 1.0m,
        int? PlannedPeopleCount = null,
        Guid? ProvisioningCorrelationId = null,
        string? ScenarioModelSetVersion = null) : IRequest<CityCreatedDto>, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.SimulationCoreClassicCityCreate;
    }
}
