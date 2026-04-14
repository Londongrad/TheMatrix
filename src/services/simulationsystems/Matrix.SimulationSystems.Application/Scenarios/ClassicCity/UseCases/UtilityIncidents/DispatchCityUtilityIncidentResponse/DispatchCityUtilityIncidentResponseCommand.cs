using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.SimulationSystems.Application.Authorization.Permissions;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.UtilityIncidents.Common;
using MediatR;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.UtilityIncidents.DispatchCityUtilityIncidentResponse
{
    public sealed record DispatchCityUtilityIncidentResponseCommand(
        Guid CityId,
        string Focus,
        string Intensity,
        bool EmergencyOverride,
        Guid? FocusDistrictId = null)
        : IRequest<CityUtilityIncidentStatusDto?>, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.SimulationSystemsClassicCityManage;
    }
}
