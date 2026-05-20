using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.SimulationSystems.Application.Authorization.Permissions;
using MediatR;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.UtilityIncidents.GetCityDistrictUtilityIncidentConditions
{
    public sealed record GetCityDistrictUtilityIncidentConditionsQuery(Guid CityId)
        : IRequest<CityDistrictUtilityIncidentConditionsDto?>, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.SimulationSystemsClassicCityRead;
    }
}
