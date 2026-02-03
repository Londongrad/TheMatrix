using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.Population.Application.Authorization.Permissions;
using MediatR;

namespace Matrix.Population.Application.UseCases.Population.SyncCityEnvironment
{
    public sealed record SyncCityEnvironmentCommand(
        Guid CityId,
        string ClimateZone,
        string Hemisphere,
        int UtcOffsetMinutes) : IRequest, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.PopulationPeopleInitialize;
    }
}
