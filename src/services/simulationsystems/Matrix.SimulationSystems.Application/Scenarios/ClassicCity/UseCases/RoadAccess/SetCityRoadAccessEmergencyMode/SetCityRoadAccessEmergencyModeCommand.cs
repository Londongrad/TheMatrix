using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.SimulationSystems.Application.Authorization.Permissions;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.RoadAccess.Common;
using MediatR;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.RoadAccess.SetCityRoadAccessEmergencyMode
{
    public sealed record SetCityRoadAccessEmergencyModeCommand(
        Guid CityId,
        bool Enabled)
        : IRequest<CityRoadAccessStatusDto?>, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.SimulationSystemsClassicCityManage;
    }
}
