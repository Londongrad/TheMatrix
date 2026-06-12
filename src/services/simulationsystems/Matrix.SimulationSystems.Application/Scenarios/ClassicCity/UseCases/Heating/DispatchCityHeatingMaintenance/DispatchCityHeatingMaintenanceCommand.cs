using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.Authorization.Permissions;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.Heating.Common;
using MediatR;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.Heating.DispatchCityHeatingMaintenance
{
    public sealed record DispatchCityHeatingMaintenanceCommand(
        Guid CityId,
        string Focus,
        string Intensity,
        bool EmergencyOverride)
        : IRequest<CityHeatingStatusDto?>, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.SimulationSystemsClassicCityManage;
    }
}
