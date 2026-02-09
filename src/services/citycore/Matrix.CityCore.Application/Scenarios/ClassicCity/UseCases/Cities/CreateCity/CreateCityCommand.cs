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
        DateTimeOffset StartSimTimeUtc,
        decimal SpeedMultiplier = 1.0m) : IRequest<CityCreatedDto>;
}
