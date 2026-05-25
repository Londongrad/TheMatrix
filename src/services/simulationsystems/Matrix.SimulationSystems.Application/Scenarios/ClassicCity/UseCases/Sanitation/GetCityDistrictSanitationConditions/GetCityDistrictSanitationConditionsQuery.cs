using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.SimulationSystems.Application.Authorization.Permissions;
using MediatR;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.Sanitation.
    GetCityDistrictSanitationConditions
{
    public sealed record GetCityDistrictSanitationConditionsQuery(Guid CityId)
        : IRequest<CityDistrictSanitationConditionsDto?>, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.SimulationSystemsClassicCityRead;
    }
}
