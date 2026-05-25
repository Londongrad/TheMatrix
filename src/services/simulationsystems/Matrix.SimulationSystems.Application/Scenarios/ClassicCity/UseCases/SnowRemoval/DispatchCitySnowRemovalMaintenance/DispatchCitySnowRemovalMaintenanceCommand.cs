using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.SimulationSystems.Application.Authorization.Permissions;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.SnowRemoval.Common;
using MediatR;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.SnowRemoval.
    DispatchCitySnowRemovalMaintenance
{
    public sealed record DispatchCitySnowRemovalMaintenanceCommand(
        Guid CityId,
        string Focus,
        string Intensity,
        bool EmergencyOverride)
        : IRequest<CitySnowRemovalStatusDto?>, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.SimulationSystemsClassicCityManage;
    }
}
