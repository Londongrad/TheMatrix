using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.CityCore.Application.Authorization.Permissions;
using MediatR;

namespace Matrix.CityCore.Application.Scenarios.ClassicCity.UseCases.Cities.UpdateCityEnvironment
{
    public sealed record UpdateCityEnvironmentCommand(
        Guid CityId,
        string ClimateZone,
        string Hemisphere,
        int UtcOffsetMinutes) : IRequest<bool>, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.CityCoreClassicCityUpdate;
    }
}
