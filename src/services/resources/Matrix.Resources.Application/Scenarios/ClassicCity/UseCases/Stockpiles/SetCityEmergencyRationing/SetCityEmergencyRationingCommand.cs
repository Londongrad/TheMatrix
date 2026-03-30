using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.Resources.Application.Authorization.Permissions;
using MediatR;

namespace Matrix.Resources.Application.Scenarios.ClassicCity.UseCases.Stockpiles.SetCityEmergencyRationing
{
    public sealed record SetCityEmergencyRationingCommand(
        Guid CityId,
        bool Enabled) : IRequest<SetCityEmergencyRationingResult>, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.ResourcesClassicCityManage;
    }
}
