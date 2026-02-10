using MediatR;

namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.SyncCityEnvironment
{
    public sealed record ApplyCityEnvironmentSyncCommand(
        Guid CityId,
        string ClimateZone,
        string Hemisphere,
        int UtcOffsetMinutes,
        DateTimeOffset? SyncedAtUtc = null) : IRequest<SyncCityEnvironmentResult>;
}
