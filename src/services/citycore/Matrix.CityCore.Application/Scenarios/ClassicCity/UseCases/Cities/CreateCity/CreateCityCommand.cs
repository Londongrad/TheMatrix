using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.CityCore.Application.Authorization.Permissions;
using MediatR;

namespace Matrix.CityCore.Application.Scenarios.ClassicCity.UseCases.Cities.CreateCity
{
    public sealed record CreateCityCommand(
        string Name,
        string? SimulationKind,
        string ClimateZone,
        string Hemisphere,
        int UtcOffsetMinutes,
        string? GenerationSeed,
        string? SizeTier,
        string? UrbanDensity,
        string? DevelopmentLevel,
        string? PopulationOccupancyProfile,
        DateTimeOffset StartSimTimeUtc,
        decimal SpeedMultiplier = 1.0m,
        int? PlannedPeopleCount = null) : IRequest<CityCreatedDto>, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.CityCoreClassicCityCreate;
    }
}
