using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.Population.Application.Authorization.Permissions;
using MediatR;

namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.SyncCityEnvironment
{
    public sealed record SyncCityEnvironmentCommand(
        Guid CityId,
        string ClimateZone,
        string Hemisphere,
        int UtcOffsetMinutes,
        DateTimeOffset? SyncedAtUtc = null) : IRequest<SyncCityEnvironmentResult>, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.PopulationPeopleInitialize;
    }
}
