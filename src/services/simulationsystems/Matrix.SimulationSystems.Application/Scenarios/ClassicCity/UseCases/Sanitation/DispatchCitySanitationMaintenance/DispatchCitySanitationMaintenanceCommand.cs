using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.SimulationSystems.Application.Authorization.Permissions;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.Sanitation.Common;
using MediatR;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.Sanitation.DispatchCitySanitationMaintenance
{
    public sealed record DispatchCitySanitationMaintenanceCommand(
        Guid CityId,
        string Focus,
        string Intensity)
        : IRequest<CitySanitationStatusDto?>, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.SimulationSystemsClassicCityManage;
    }
}
