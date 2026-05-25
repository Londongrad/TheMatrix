using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.SimulationSystems.Application.Authorization.Permissions;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.SnowRemoval.Common;
using MediatR;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.SnowRemoval.
    SetCitySnowRemovalEmergencyMode
{
    public sealed record SetCitySnowRemovalEmergencyModeCommand(
        Guid CityId,
        bool Enabled)
        : IRequest<CitySnowRemovalStatusDto?>, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.SimulationSystemsClassicCityManage;
    }
}
